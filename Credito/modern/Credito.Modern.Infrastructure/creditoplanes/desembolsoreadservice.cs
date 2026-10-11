using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class DesembolsoReadService(IOptions<SqlDatabaseOptions> options) : IDesembolsoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<DesembolsoPendienteRowDto>> ListarPendientesAsync(
        int oficinaId,
        int usuarioRegId,
        int personaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        if (personaId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "personaId debe ser >= 0.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                c.CreditoId,
                p.Codigo AS PersonaCodigo,
                p.NombreCompleto AS PersonaNombre,
                c.MontoCredito,
                c.MontoGastosAdm,
                c.MontoDesembolso,
                c.Estado
            FROM CREDITO.Credito AS c
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
            WHERE c.Estado = 'APR'
              AND c.OficinaId = @OficinaId
              AND (
                    (@PersonaId = 0 AND c.UsuarioRegId = @UsuarioRegId)
                 OR (@PersonaId > 0 AND c.PersonaId = @PersonaId)
              )
            ORDER BY c.CreditoId;
            """;
        var command = new CommandDefinition(
            sql,
            new { OficinaId = oficinaId, UsuarioRegId = usuarioRegId, PersonaId = personaId },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DesembolsoPendienteRowDto>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<CreditoGestorPendienteRowDto>> ListarCreditosGestorDesembolsadosAsync(
        int usuarioRegId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        // Set-based (CTE): evita N subconsultas correlacionadas por crédito (lento en Azure Basic).
        const string sql = """
            DECLARE @Hoy date = CAST(dbo.ufnFecha() AS date);

            ;WITH PlanAgg AS
            (
                SELECT
                    pp.CreditoId,
                    SUM(CASE WHEN pp.Estado <> N'CAN' THEN ISNULL(pp.ImporteMora, 0) ELSE 0 END)
                        AS ImporteMora,
                    SUM(
                        CASE
                            WHEN pp.Estado NOT IN (N'PAG', N'CAN')
                            THEN ISNULL(pp.Cuota, 0) + ISNULL(pp.Cargo, 0)
                                 - ISNULL(pp.PagoLibre, 0) - ISNULL(pp.Descuento, 0)
                            ELSE 0
                        END
                    ) AS CapitalPendiente,
                    SUM(
                        CASE
                            WHEN pp.Estado NOT IN (N'PAG', N'CAN')
                             AND pp.FechaVencimiento < @Hoy
                            THEN ISNULL(pp.Cuota, 0) + ISNULL(pp.Cargo, 0)
                                 - ISNULL(pp.PagoLibre, 0) - ISNULL(pp.Descuento, 0)
                                 + ISNULL(pp.ImporteMora, 0)
                            ELSE 0
                        END
                    ) AS VencidasAcum,
                    MIN(CASE WHEN pp.Estado = N'PEN' THEN pp.FechaVencimiento END) AS MinVenPen
                FROM CREDITO.PlanPago AS pp
                INNER JOIN CREDITO.Credito AS c0
                    ON c0.CreditoId = pp.CreditoId
                WHERE c0.UsuarioRegId = @UsuarioRegId
                  AND c0.Estado = N'DES'
                GROUP BY pp.CreditoId
            ),
            PrimeraPendiente AS
            (
                SELECT
                    x.CreditoId,
                    x.MontoCuota
                FROM (
                    SELECT
                        pp.CreditoId,
                        ISNULL(pp.Cuota, 0) + ISNULL(pp.Cargo, 0)
                            - ISNULL(pp.PagoLibre, 0) - ISNULL(pp.Descuento, 0)
                            + ISNULL(pp.ImporteMora, 0) AS MontoCuota,
                        ROW_NUMBER() OVER (
                            PARTITION BY pp.CreditoId
                            ORDER BY pp.FechaVencimiento ASC, pp.Numero ASC
                        ) AS rn
                    FROM CREDITO.PlanPago AS pp
                    INNER JOIN CREDITO.Credito AS c1
                        ON c1.CreditoId = pp.CreditoId
                    WHERE c1.UsuarioRegId = @UsuarioRegId
                      AND c1.Estado = N'DES'
                      AND pp.Estado NOT IN (N'PAG', N'CAN')
                ) AS x
                WHERE x.rn = 1
            )
            SELECT
                c.CreditoId,
                p.Codigo AS PersonaCodigo,
                p.NombreCompleto AS PersonaNombre,
                c.MontoCredito,
                c.PersonaId,
                c.FechaVencimiento,
                CAST(ISNULL(pa.ImporteMora, 0) AS decimal(18, 2)) AS ImporteMora,
                CAST(
                    ISNULL(pa.CapitalPendiente, 0) + ISNULL(pa.ImporteMora, 0)
                    AS decimal(18, 2)
                ) AS DeudaPendiente,
                TRY_CAST(SUBSTRING(p.Codigo, 3, LEN(p.Codigo)) AS int) AS Orden,
                NULLIF(LTRIM(RTRIM(p.Celular1)), N'') AS Celular,
                NULLIF(LTRIM(RTRIM(p.Direccion)), N'') AS Direccion,
                CASE
                    WHEN cl.Latitud IS NULL OR cl.Longitud IS NULL THEN NULL
                    WHEN cl.Latitud = 0 OR cl.Longitud = 0 THEN NULL
                    ELSE cl.Latitud
                END AS Latitud,
                CASE
                    WHEN cl.Latitud IS NULL OR cl.Longitud IS NULL THEN NULL
                    WHEN cl.Latitud = 0 OR cl.Longitud = 0 THEN NULL
                    ELSE cl.Longitud
                END AS Longitud,
                CAST(
                    CASE
                        WHEN ISNULL(pa.VencidasAcum, 0) > 0 THEN pa.VencidasAcum
                        ELSE ISNULL(fp.MontoCuota, 0)
                    END
                    AS decimal(18, 2)
                ) AS CuotaSugerida,
                ISNULL(dbo.ufnCalcularDiasAtrazo(pa.MinVenPen, @Hoy), 0) AS DiasAtrazo
            FROM CREDITO.Credito AS c
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
            LEFT JOIN MAESTRO.Cliente AS cl ON cl.PersonaId = c.PersonaId
            LEFT JOIN PlanAgg AS pa ON pa.CreditoId = c.CreditoId
            LEFT JOIN PrimeraPendiente AS fp ON fp.CreditoId = c.CreditoId
            WHERE c.UsuarioRegId = @UsuarioRegId
              AND c.Estado = N'DES'
            ORDER BY
                CASE
                    WHEN c.FechaVencimiento < @Hoy THEN 0
                    WHEN ISNULL(dbo.ufnCalcularDiasAtrazo(pa.MinVenPen, @Hoy), 0) > 0 THEN 0
                    ELSE 1
                END,
                c.FechaVencimiento ASC,
                p.NombreCompleto ASC,
                c.CreditoId ASC;
            """;
        var rows = await connection
            .QueryAsync<CreditoGestorPendienteRowDto>(
                new CommandDefinition(sql, new { UsuarioRegId = usuarioRegId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<ValidarDesembolsoResponse> ValidarAsync(
        int cajaDiarioId,
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cxcPendientes = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CuentaxCobrar
                WHERE CreditoId = @CreditoId
                  AND Estado = 'PEN';
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (cxcPendientes > 0)
        {
            return new ValidarDesembolsoResponse(false, "Tiene Cuentas por cobrar Pendientes!");
        }

        var montoDesembolso = await connection.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                "SELECT MontoDesembolso FROM CREDITO.Credito WHERE CreditoId = @CreditoId;",
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (montoDesembolso is null)
        {
            return new ValidarDesembolsoResponse(false, "No existe el crédito indicado.");
        }

        var caja = await connection.QueryFirstOrDefaultAsync<CajaSaldoRow>(
            new CommandDefinition(
                """
                SELECT SaldoFinal, IndCierre
                FROM CREDITO.CajaDiario
                WHERE CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (caja is null)
        {
            return new ValidarDesembolsoResponse(false, "No existe la caja diario indicada.");
        }

        if (caja.IndCierre)
        {
            return new ValidarDesembolsoResponse(false, "La caja diario está cerrada.");
        }

        if (montoDesembolso.Value > caja.SaldoFinal)
        {
            return new ValidarDesembolsoResponse(false, "Saldo Insuficiente!");
        }

        return new ValidarDesembolsoResponse(true, null);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class CajaSaldoRow
    {
        public decimal SaldoFinal { get; init; }
        public bool IndCierre { get; init; }
    }
}
