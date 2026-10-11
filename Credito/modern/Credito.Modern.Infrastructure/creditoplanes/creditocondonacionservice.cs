using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoCondonacionService(IOptions<SqlDatabaseOptions> options)
    : ICreditoCondonacionService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<SolicitarCondonacionResponse> SolicitarAsync(
        SolicitarCondonacionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "oficinaId debe ser >= 1.");
        }

        if (request.CajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "cajaDiarioId debe ser >= 1.");
        }

        if (request.CreditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "creditoId debe ser >= 1.");
        }

        if (request.MoraCondonacion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Indique un monto de mora mayor a 0 para solicitar la condonación.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await EnsureCondonacionTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var cajaOk = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
                WHERE cd.CajaDiarioId = @CajaDiarioId
                  AND ca.OficinaId = @OficinaId
                  AND cd.IndCierre = CAST(0 AS bit);
                """,
                new { request.CajaDiarioId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (cajaOk == 0)
        {
            throw new InvalidOperationException(
                "La caja diario no está abierta o no pertenece a la oficina de la sesión.");
        }

        var creditoOk = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.Credito
                WHERE CreditoId = @CreditoId
                  AND OficinaId = @OficinaId;
                """,
                new { request.CreditoId, request.OficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (creditoOk == 0)
        {
            throw new KeyNotFoundException($"No existe el crédito {request.CreditoId} en esta oficina.");
        }

        var yaPendiente = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CreditoCondonacion
                WHERE CreditoId = @CreditoId
                  AND IndAprobado = CAST(0 AS bit);
                """,
                new { request.CreditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (yaPendiente > 0)
        {
            throw new InvalidOperationException(
                "Ya existe una solicitud de condonación pendiente para este crédito. Revísela en Condonaciones o en la ficha del crédito.");
        }

        // Insert null-safe (evita TotalPago NULL cuando no hay pagos CUO / Interes nulo).
        // Devuelve Id para no depender del rowcount de batches Dapper (-1).
        int? newId;
        try
        {
            newId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    DECLARE @MontoCredito decimal(15, 2) = (
                        SELECT
                            ISNULL(MontoCredito, 0)
                            + ISNULL(MontoCredito, 0) * ISNULL(Interes, 0) / 100.0
                        FROM CREDITO.Credito
                        WHERE CreditoId = @CreditoId
                    );

                    IF @MontoCredito IS NULL
                        THROW 50001, N'No existe el crédito indicado para condonación.', 1;

                    DECLARE @Pagos decimal(15, 2) = ISNULL((
                        SELECT SUM(ImportePago)
                        FROM CREDITO.MovimientoCaja
                        WHERE CreditoId = @CreditoId
                          AND ImportePago > 0
                          AND Operacion = N'CUO'
                    ), 0);

                    INSERT CREDITO.CreditoCondonacion
                    (
                        CreditoId,
                        CajaDiarioId,
                        Fecha,
                        MoraCondonacion,
                        IndAprobado,
                        TotalPago
                    )
                    VALUES
                    (
                        @CreditoId,
                        @CajaDiarioId,
                        dbo.ufnFecha(),
                        @MoraCondonacion,
                        CAST(0 AS bit),
                        @MontoCredito - @Pagos + @MoraCondonacion
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        request.CajaDiarioId,
                        request.CreditoId,
                        request.MoraCondonacion,
                    },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number is 208 or 207)
        {
            throw new InvalidOperationException(
                "Falta la tabla CREDITO.CreditoCondonacion en la base. Ejecute el script deploy/sql/2026-09-10-credito-condonacion.sql en Azure SQL.",
                ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"No se pudo registrar la condonación en SQL: {TrimSql(ex.Message)}",
                ex);
        }

        if (newId is null or < 1)
        {
            throw new InvalidOperationException(
                "No se pudo insertar la solicitud de condonación. Verifique el crédito y la caja.");
        }

        return new SolicitarCondonacionResponse(true, null);
    }

    public async Task<IReadOnlyList<CondonacionPendienteDto>> ListarPendientesAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCondonacionTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<CondonacionPendienteDto>(
            new CommandDefinition(
                """
                SELECT cc.Id,
                       cc.CreditoId,
                       c.PersonaId,
                       ISNULL(p.NombreCompleto, N'') AS NombreCliente,
                       ISNULL(u.NombreUsuario, N'') AS NombreUsuario,
                       c.MontoCredito,
                       cc.MoraCondonacion,
                       cc.TotalPago,
                       cc.Fecha,
                       cc.CajaDiarioId
                FROM CREDITO.CreditoCondonacion AS cc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
                WHERE cc.IndAprobado = CAST(0 AS bit)
                  AND c.OficinaId = @OficinaId
                ORDER BY cc.Id DESC;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.AsList();
    }

    public async Task<CondonacionPendienteCreditoDto> ObtenerPendientePorCreditoAsync(
        int oficinaId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCondonacionTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var row = await connection.QueryFirstOrDefaultAsync<PendienteRow>(
            new CommandDefinition(
                """
                SELECT TOP (1)
                       cc.Id,
                       cc.MoraCondonacion,
                       cc.TotalPago
                FROM CREDITO.CreditoCondonacion AS cc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                WHERE cc.CreditoId = @CreditoId
                  AND c.OficinaId = @OficinaId
                  AND cc.IndAprobado = CAST(0 AS bit)
                ORDER BY cc.Id DESC;
                """,
                new { CreditoId = creditoId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return new CondonacionPendienteCreditoDto(false, 0m, 0m, null);
        }

        return new CondonacionPendienteCreditoDto(true, row.MoraCondonacion, row.TotalPago, row.Id);
    }

    public async Task EliminarAsync(
        int oficinaId,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (id < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "id debe ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCondonacionTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var filas = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE cc
                FROM CREDITO.CreditoCondonacion AS cc
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = cc.CreditoId
                WHERE cc.Id = @Id
                  AND c.OficinaId = @OficinaId
                  AND cc.IndAprobado = CAST(0 AS bit);
                """,
                new { Id = id, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (filas == 0)
        {
            throw new KeyNotFoundException("No existe la solicitud pendiente en esta oficina.");
        }
    }

    private static async Task EnsureCondonacionTableAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF OBJECT_ID(N'CREDITO.CreditoCondonacion', N'U') IS NULL
                BEGIN
                    CREATE TABLE CREDITO.CreditoCondonacion (
                        Id int IDENTITY(1,1) NOT NULL
                            CONSTRAINT PK_CreditoCondonacion PRIMARY KEY,
                        CreditoId int NOT NULL,
                        CajaDiarioId int NOT NULL,
                        Fecha datetime NOT NULL,
                        MoraCondonacion decimal(10,2) NOT NULL,
                        TotalPago decimal(15,2) NOT NULL,
                        IndAprobado bit NOT NULL,
                        CONSTRAINT FK_CreditoCondonacion_Credito FOREIGN KEY (CreditoId)
                            REFERENCES CREDITO.Credito (CreditoId),
                        CONSTRAINT FK_CreditoCondonacion_CajaDiario FOREIGN KEY (CajaDiarioId)
                            REFERENCES CREDITO.CajaDiario (CajaDiarioId)
                    );
                END
                """,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static string TrimSql(string message)
    {
        var line = message.Split('\n', 2)[0].Trim();
        return line.Length > 160 ? line[..160] + "…" : line;
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class PendienteRow
    {
        public int Id { get; init; }
        public decimal MoraCondonacion { get; init; }
        public decimal TotalPago { get; init; }
    }
}
