using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaDiarioOperacionWriteService(IOptions<SqlDatabaseOptions> options)
    : ICajaDiarioOperacionWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<CajaDiarioOperacionResponse> ReconciliarCajaDiarioAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default)
    {
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        return ExecuteIntProcAsync(
            "CREDITO.usp_ReconciliarCajaDiario",
            new { CajaDiarioId = cajaDiarioId },
            cajaDiarioId,
            cancellationToken);
    }

    public Task<CajaDiarioOperacionResponse> RecalcularCajaDiarioAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default)
    {
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        return ExecuteIntProcAsync(
            "CREDITO.usp_RecalcularCajaDiario",
            new { CajaDiarioId = cajaDiarioId },
            cajaDiarioId,
            cancellationToken);
    }

    public Task<CajaDiarioOperacionResponse> CerrarCajasDiariosAsync(
        int usuarioCierreId,
        int oficinaId,
        decimal sobrante,
        CancellationToken cancellationToken = default)
    {
        if (usuarioCierreId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioCierreId), "usuarioCierreId debe ser >= 1.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        return ExecuteIntProcAsync(
            "CREDITO.usp_CerrarCajasDiarios",
            new
            {
                UsuarioCierreId = usuarioCierreId,
                OficinaId = oficinaId,
                Sobrante = sobrante,
            },
            cajaDiarioId: null,
            cancellationToken);
    }

    private async Task<CajaDiarioOperacionResponse> ExecuteIntProcAsync(
        string procedureName,
        object parameters,
        int? cajaDiarioId,
        CancellationToken cancellationToken)
    {
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
            var dynamicParams = new DynamicParameters(parameters);
            dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            var command = new CommandDefinition(
                procedureName,
                dynamicParams,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command).ConfigureAwait(false);
            var resultCode = dynamicParams.Get<int>("ReturnValue");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CajaDiarioOperacionResponse(resultCode, cajaDiarioId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
