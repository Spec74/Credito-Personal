using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CompletarImpagosValidacionReadService(IOptions<SqlDatabaseOptions> options)
    : ICompletarImpagosValidacionReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CompletarImpagosValidacionResponse> ValidarAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_CompletarImpagosValidacion",
            new { CajaDiarioId = cajaDiarioId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var valor = await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
        return new CompletarImpagosValidacionResponse(valor);
    }
}
