using System.Data;
using Credito.Modern.Application.Prendario;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SqlDatabaseOptions = Credito.Modern.Infrastructure.SqlDatabaseOptions;

namespace Credito.Modern.Infrastructure.Prendario;

public sealed class PrendarioWriteService(IOptions<SqlDatabaseOptions> options) : IPrendarioWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<DevolverPrendaResponse> DevolverAsync(
        DevolverPrendaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (request.PrendaId < 1 || request.OficinaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "oficinaId, prendaId y usuario deben ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var row = await connection.QuerySingleAsync<DevolverPrendaResponse>(
                new CommandDefinition(
                    "CREDITO.usp_DevolverPrenda",
                    new
                    {
                        request.PrendaId,
                        UsuarioId = usuarioId,
                        request.OficinaId,
                    },
                    transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return row;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
