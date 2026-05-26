using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoCajaScopeReadService(IOptions<SqlDatabaseOptions> options)
    : IMovimientoCajaScopeReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoCajaScopeDto?> GetScopeAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                c.OficinaId,
                mc.CajaDiarioId,
                mc.Operacion,
                mc.Estado AS EstadoActivo,
                cd.IndCierre AS CajaDiarioCerrada
            FROM CREDITO.MovimientoCaja AS mc
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            WHERE mc.MovimientoCajaId = @MovimientoCajaId;
            """;
        var command = new CommandDefinition(
            sql,
            new { MovimientoCajaId = movimientoCajaId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<MovimientoCajaScopeDto>(command).ConfigureAwait(false);
    }

    public async Task<bool> RequiereConfirmacionAnularAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN CREDITO.CuentaxCobrar AS cx ON cx.MovimientoCajaId = mc.MovimientoCajaId
                INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = cx.CreditoId
                WHERE mc.MovimientoCajaId = @MovimientoCajaId
                  AND mc.Operacion = 'INI'
                  AND cx.CreditoId IS NOT NULL
                  AND pp.Estado = 'PAG'
            ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
            """;
        var command = new CommandDefinition(
            sql,
            new { MovimientoCajaId = movimientoCajaId },
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
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
