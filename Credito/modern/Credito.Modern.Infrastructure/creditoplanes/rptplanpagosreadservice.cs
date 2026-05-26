using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptPlanPagosReadService(IOptions<SqlDatabaseOptions> options) : IRptPlanPagosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<RptPlanPagosRowDto>> ListarAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            return Array.Empty<RptPlanPagosRowDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<RptPlanPagosRowDto>(
            new CommandDefinition(
                """
                SELECT
                    pp.Numero,
                    pp.Capital,
                    pp.FechaVencimiento AS FechaPago,
                    pp.Amortizacion,
                    pp.Interes,
                    pp.GastosAdm,
                    pp.Cuota
                FROM CREDITO.PlanPago AS pp
                WHERE pp.CreditoId = @CreditoId
                ORDER BY pp.Numero;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
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
