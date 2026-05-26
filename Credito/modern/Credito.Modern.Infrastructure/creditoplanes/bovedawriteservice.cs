using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaWriteService(IOptions<SqlDatabaseOptions> options) : IBovedaWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<CajaDiarioOperacionResponse> CerrarBovedaAsync(
        int oficinaId,
        int usuarioRegId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        return ExecuteIntProcAsync(
            "CREDITO.usp_CerrarBoveda",
            new { OficinaId = oficinaId, UsuarioRegId = usuarioRegId },
            cancellationToken);
    }

    public Task<CajaDiarioOperacionResponse> CerrarBovedaTemporalAsync(
        int oficinaId,
        int usuarioRegId,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId), "usuarioRegId debe ser >= 1.");
        }

        return ExecuteIntProcAsync(
            "CREDITO.usp_CerrarBovedaTemporal",
            new { OficinaId = oficinaId, UsuarioRegId = usuarioRegId },
            cancellationToken);
    }

    public Task<CajaDiarioOperacionResponse> TransferirBovedaAsync(
        int bovedaInicioId,
        int bovedaDestinoId,
        string glosa,
        decimal monto,
        int usuarioRegId,
        int flagAceptar,
        int bovedaMovTempId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteIntProcAsync(
            "CREDITO.usp_TransferirBoveda",
            new
            {
                BovedaInicioId = bovedaInicioId,
                BovedaDestinoId = bovedaDestinoId,
                Glosa = glosa ?? string.Empty,
                Monto = monto,
                UsuarioRegId = usuarioRegId,
                flagAceptar = flagAceptar,
                BovedaMovTempId = bovedaMovTempId,
            },
            cancellationToken);
    }

    private async Task<CajaDiarioOperacionResponse> ExecuteIntProcAsync(
        string procedureName,
        object parameters,
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
            return new CajaDiarioOperacionResponse(resultCode);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
