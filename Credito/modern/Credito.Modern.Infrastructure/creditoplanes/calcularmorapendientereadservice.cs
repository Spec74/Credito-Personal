using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CalcularMoraPendienteReadService(IOptions<SqlDatabaseOptions> options)
    : ICalcularMoraPendienteReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<decimal?> ObtenerAsync(int creditoId, CancellationToken cancellationToken = default)
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
        var command = new CommandDefinition(
            "CREDITO.usp_CalcularMoraPendiente",
            new { CreditoId = creditoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<decimal?>(command).ConfigureAwait(false);
    }
}
