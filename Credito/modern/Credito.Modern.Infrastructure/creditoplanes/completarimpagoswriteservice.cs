using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CompletarImpagosWriteService(IOptions<SqlDatabaseOptions> options) : ICompletarImpagosWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CompletarImpagosResponse> EjecutarAsync(
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
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var command = new CommandDefinition(
                "CREDITO.usp_CompletarImpagos",
                new { CajaDiarioId = cajaDiarioId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CompletarImpagosResponse(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
