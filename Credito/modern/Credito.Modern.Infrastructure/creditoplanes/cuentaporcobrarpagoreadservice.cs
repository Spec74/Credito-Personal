using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CuentaPorCobrarPagoReadService(IOptions<SqlDatabaseOptions> options)
    : ICuentaPorCobrarPagoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<CuentaPorCobrarPendienteRowDto>> ListarPendientesAsync(
        int oficinaId,
        int cajaDiarioId,
        int usuarioRegId,
        int personaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        ValidateListParams(oficinaId, cajaDiarioId, usuarioRegId, personaId);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var esCajaCentral = await EsCajaCentralAsync(connection, cajaDiarioId, cancellationToken)
            .ConfigureAwait(false);

        // CxC GAD guarda Monto = MontoGastosAdm + CentralRiesgo (usp_Credito_Upd).
        // Concepto deja explícito GAD puro vs suma con central de riesgo.
        const string sqlCredito = """
            SELECT
                ISNULL(cr.OrdenVentaId, 0) AS OrdenVentaId,
                cx.CuentaxCobrarId,
                p.Codigo AS PersonaCodigo,
                p.NombreCompleto AS PersonaNombre,
                cx.Operacion,
                CASE
                    WHEN cx.Operacion = N'GAD' AND ISNULL(cr.CentralRiesgo, 0) > 0
                        THEN CONCAT(
                            N'GAD ',
                            CONVERT(varchar(24), CAST(ISNULL(cr.MontoGastosAdm, 0) AS decimal(16, 2))),
                            N' + central riesgo ',
                            CONVERT(varchar(24), CAST(cr.CentralRiesgo AS decimal(16, 2))))
                    WHEN cx.Operacion = N'GAD'
                        THEN N'GAD puro'
                    WHEN cx.Operacion = N'INI'
                        THEN N'Inicial'
                    ELSE cx.Operacion
                END AS Concepto,
                (N'CREDITO: ' + CAST(cx.CreditoId AS varchar(20))) AS Origen,
                cx.Monto,
                ISNULL(cr.MontoGastosAdm, 0) AS MontoGastosAdm,
                ISNULL(cr.CentralRiesgo, 0) AS CentralRiesgo,
                cx.Estado,
                ISNULL(cr.FechaAprobacion, cr.FechaReg) AS FechaReg
            FROM CREDITO.CuentaxCobrar AS cx
            INNER JOIN CREDITO.Credito AS cr ON cr.CreditoId = cx.CreditoId
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = cr.PersonaId
            WHERE cx.Estado = 'PEN'
              AND cr.OficinaId = @OficinaId
              AND (
                    (@PersonaId = 0 AND cr.UsuarioRegId = @UsuarioRegId)
                 OR (
                        @PersonaId > 0
                    AND cr.PersonaId = @PersonaId
                    AND (@EsCajaCentral = 1 OR cr.UsuarioRegId = @UsuarioRegId)
                    )
              );
            """;

        var param = new
        {
            OficinaId = oficinaId,
            UsuarioRegId = usuarioRegId,
            PersonaId = personaId,
            EsCajaCentral = esCajaCentral ? 1 : 0,
        };

        var creditoRows = (await connection
            .QueryAsync<CuentaPorCobrarPendienteRowDto>(
                new CommandDefinition(sqlCredito, param, cancellationToken: cancellationToken))
            .ConfigureAwait(false)).AsList();

        if (personaId <= 0)
        {
            return creditoRows;
        }

        const string sqlOrden = """
            SELECT
                ov.OrdenVentaId,
                0 AS CuentaxCobrarId,
                p.Codigo AS PersonaCodigo,
                p.NombreCompleto AS PersonaNombre,
                'CON' AS Operacion,
                N'Venta contado' AS Concepto,
                (N'ORDEN: ' + CAST(ov.OrdenVentaId AS varchar(20))) AS Origen,
                ov.TotalNeto AS Monto,
                CAST(0 AS decimal(16, 2)) AS MontoGastosAdm,
                CAST(0 AS decimal(16, 2)) AS CentralRiesgo,
                'PEN' AS Estado,
                ov.FechaReg
            FROM VENTAS.OrdenVenta AS ov
            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = ov.PersonaId
            WHERE ov.PersonaId = @PersonaId
              AND ov.TipoVenta = 'CON'
              AND ov.Estado = 'ENV'
              AND ov.OficinaId = @OficinaId;
            """;

        var ordenRows = await connection
            .QueryAsync<CuentaPorCobrarPendienteRowDto>(
                new CommandDefinition(
                    sqlOrden,
                    new { PersonaId = personaId, OficinaId = oficinaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        creditoRows.AddRange(ordenRows);
        return creditoRows;
    }

    public async Task<bool> TienePendientesPorCreditoAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CuentaxCobrar
                WHERE CreditoId = @CreditoId
                  AND Estado = 'PEN';
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return count > 0;
    }

    public async Task<bool> PuedeCobrarAsync(
        int oficinaId,
        int ordenVentaId,
        int cuentaxCobrarId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (cuentaxCobrarId > 0)
        {
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT CASE WHEN EXISTS (
                        SELECT 1
                        FROM CREDITO.CuentaxCobrar AS cx
                        INNER JOIN CREDITO.Credito AS cr ON cr.CreditoId = cx.CreditoId
                        WHERE cx.CuentaxCobrarId = @CuentaxCobrarId
                          AND cx.Estado = 'PEN'
                          AND cr.OficinaId = @OficinaId
                          AND (
                                @OrdenVentaId = 0
                             OR ISNULL(cr.OrdenVentaId, 0) = @OrdenVentaId
                          )
                    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                    """,
                    new { CuentaxCobrarId = cuentaxCobrarId, OficinaId = oficinaId, OrdenVentaId = ordenVentaId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        if (ordenVentaId < 1)
        {
            return false;
        }

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM VENTAS.OrdenVenta AS ov
                    WHERE ov.OrdenVentaId = @OrdenVentaId
                      AND ov.OficinaId = @OficinaId
                      AND ov.TipoVenta = 'CON'
                      AND ov.Estado = 'ENV'
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { OrdenVentaId = ordenVentaId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task<bool> EsCajaCentralAsync(
        SqlConnection connection,
        int cajaDiarioId,
        CancellationToken cancellationToken)
    {
        var denominacion = await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                """
                SELECT c.Denominacion
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                WHERE cd.CajaDiarioId = @CajaDiarioId;
                """,
                new { CajaDiarioId = cajaDiarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return denominacion?.Contains("CAJA CENTRAL", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void ValidateListParams(int oficinaId, int cajaDiarioId, int usuarioRegId, int personaId)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        if (personaId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "personaId debe ser >= 0.");
        }
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
