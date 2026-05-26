using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptMovimientoCreditoReadService(IOptions<SqlDatabaseOptions> options)
    : IRptMovimientoCreditoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptMovimientoCreditoRowDto>> ListarPorCreditoAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
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
            "CREDITO.usp_RptMovimientoCredito",
            new { CreditoId = creditoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptMovimientoCreditoRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
