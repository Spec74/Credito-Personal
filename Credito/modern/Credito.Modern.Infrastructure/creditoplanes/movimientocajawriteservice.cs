using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoCajaWriteService(IOptions<SqlDatabaseOptions> options) : IMovimientoCajaWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<AnularMovimientoCajaResponse> AnularAsync(
        int movimientoCajaId,
        string observacion,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoCajaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movimientoCajaId), "movimientoCajaId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
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
            var dynamicParams = new DynamicParameters(new
            {
                MovimientoCajaId = movimientoCajaId,
                Observacion = observacion ?? string.Empty,
                UsuarioId = usuarioId,
            });
            dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            var command = new CommandDefinition(
                "CREDITO.usp_MovimientoCaja_Del",
                dynamicParams,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command).ConfigureAwait(false);
            var resultCode = dynamicParams.Get<int>("ReturnValue");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new AnularMovimientoCajaResponse(resultCode, movimientoCajaId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
