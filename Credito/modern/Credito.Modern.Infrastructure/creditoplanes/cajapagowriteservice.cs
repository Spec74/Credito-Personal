using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaPagoWriteService(IOptions<SqlDatabaseOptions> options) : ICajaPagoWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<PagoCajaResultResponse> PagarCuotasAsync(
        int cajaDiarioId,
        int creditoId,
        string listaPlanPagoId,
        decimal importeRecibido,
        int usuarioId,
        DateTime fechaPago,
        int tipoPagoId,
        string fechaPagoTransferencia,
        CancellationToken cancellationToken = default) =>
        ExecuteScalarProcAsync(
            "CREDITO.usp_PagarCuotas",
            new
            {
                CajaDiarioId = cajaDiarioId,
                CreditoId = creditoId,
                ListaPlanPagoId = listaPlanPagoId,
                ImporteRecibido = importeRecibido,
                UsuarioId = usuarioId,
                FechaPago = fechaPago.Date,
                TipoPagoId = tipoPagoId,
                FechaPagoTransferencia = fechaPagoTransferencia ?? string.Empty,
            },
            cancellationToken);

    public Task<PagoCajaResultResponse> PagarCuotaPagoLibreAsync(
        int cajaDiarioId,
        int creditoId,
        decimal importeRecibido,
        int usuarioId,
        int tipoPagoId,
        string fechaPagoTransferencia,
        CancellationToken cancellationToken = default) =>
        ExecuteScalarProcAsync(
            "CREDITO.usp_PagarCuotaPagoLibre",
            new
            {
                CajaDiarioId = cajaDiarioId,
                CreditoId = creditoId,
                ImporteRecibido = importeRecibido,
                UsuarioId = usuarioId,
                TipoPagoId = tipoPagoId,
                FechaPagoTransferencia = fechaPagoTransferencia ?? string.Empty,
            },
            cancellationToken);

    public Task<PagoCajaResultResponse> PagarCuotasCancelacionAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        DateTime fechaPago,
        CancellationToken cancellationToken = default) =>
        ExecuteScalarProcAsync(
            "CREDITO.usp_PagarCuotasCancelacion",
            new
            {
                CajaDiarioId = cajaDiarioId,
                CreditoId = creditoId,
                UsuarioId = usuarioId,
                FechaPago = fechaPago.Date,
            },
            cancellationToken);

    private async Task<PagoCajaResultResponse> ExecuteScalarProcAsync(
        string procedureName,
        object parameters,
        CancellationToken cancellationToken)
    {
        EnsureConnectionConfigured();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var command = new CommandDefinition(
                procedureName,
                parameters,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            var resultId = await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new PagoCajaResultResponse(resultId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private void EnsureConnectionConfigured()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
