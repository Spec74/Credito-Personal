using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoOficinaReadService(IOptions<SqlDatabaseOptions> options) : ICreditoOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByCreditoIdAsync(int creditoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (creditoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(creditoId), "creditoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = "SELECT OficinaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId;";
        var command = new CommandDefinition(
            sql,
            new { CreditoId = creditoId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
    }
}
