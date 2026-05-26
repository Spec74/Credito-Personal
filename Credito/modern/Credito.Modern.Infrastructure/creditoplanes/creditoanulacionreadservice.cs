using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoAnulacionReadService(IOptions<SqlDatabaseOptions> options) : ICreditoAnulacionReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ValidarAnularCreditoResponse> ValidarAnularAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cuotasPagadas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId
                  AND Estado = 'PAG';
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (cuotasPagadas > 0)
        {
            return new ValidarAnularCreditoResponse(false);
        }

        var totalCxC = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CuentaxCobrar
                WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var cxCAnuPen = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.CuentaxCobrar
                WHERE CreditoId = @CreditoId
                  AND Estado IN ('ANU', 'PEN');
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return new ValidarAnularCreditoResponse(totalCxC > 0 && totalCxC == cxCAnuPen);
    }
}
