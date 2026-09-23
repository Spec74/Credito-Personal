using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class SaldosCierreWriteService(IOptions<SqlDatabaseOptions> options) : ISaldosCierreWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task ActualizarDatosPostCierreBovedaAsync(
        int oficinaId,
        int usuarioCierreId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioCierreId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioCierreId), "usuarioCierreId debe ser >= 1.");
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
            await ExecuteProcAsync(
                connection,
                transaction,
                "CREDITO.usp_ActualizarSaldoCartera",
                new { OficinaId = oficinaId },
                cancellationToken).ConfigureAwait(false);

            await ExecuteProcAsync(
                connection,
                transaction,
                "CREDITO.usp_CalificarCliente",
                new { OficinaId = oficinaId },
                cancellationToken).ConfigureAwait(false);

            // Fuera de fin de mes / contingencia días 1–2 el SP no modifica nada.
            await ExecuteProcAsync(
                connection,
                transaction,
                "CREDITO.usp_IntentarGenerarCierreGerencialMensual",
                new { OficinaId = oficinaId, UsuarioCierreId = usuarioCierreId },
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task ExecuteProcAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string procedureName,
        object parameters,
        CancellationToken cancellationToken)
    {
        var dynamicParams = new DynamicParameters(parameters);
        dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
        await connection.ExecuteAsync(
            new CommandDefinition(
                procedureName,
                dynamicParams,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
