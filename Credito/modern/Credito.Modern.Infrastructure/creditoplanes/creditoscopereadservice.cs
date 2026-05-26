using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoScopeReadService(IOptions<SqlDatabaseOptions> options) : ICreditoScopeReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CreditoScopeDto?> GetScopeAsync(int creditoId, CancellationToken cancellationToken = default)
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
        return await connection.QueryFirstOrDefaultAsync<CreditoScopeDto>(
            new CommandDefinition(
                """
                SELECT CreditoId, OficinaId, Estado, OrdenVentaId
                FROM CREDITO.Credito
                WHERE CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
