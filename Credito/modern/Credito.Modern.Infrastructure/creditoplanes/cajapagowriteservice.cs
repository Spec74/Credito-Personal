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
        ExecutePagarCuotasWithExtensionAsync(
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
            tipoPagoId,
            fechaPagoTransferencia ?? string.Empty,
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

    /// <summary>
    /// Paridad operativa: si TipoPagoId &gt; 1, el movimiento debe tener Extension
    /// (Verificar pagos / bloqueo de cierre). El SP histórico solo lo hacía en pago libre;
    /// aquí se garantiza también para cobro de cuotas (idempotente si el SP ya lo insertó).
    /// </summary>
    private async Task<PagoCajaResultResponse> ExecutePagarCuotasWithExtensionAsync(
        string procedureName,
        object parameters,
        int tipoPagoId,
        string fechaPagoTransferencia,
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

            if (tipoPagoId > 1 && resultId is > 0)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        IF NOT EXISTS (
                            SELECT 1 FROM CREDITO.MovimientoCajaExtension
                            WHERE MovimientoCajaId = @MovimientoCajaId
                        )
                        INSERT INTO CREDITO.MovimientoCajaExtension
                            (MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
                        VALUES (@MovimientoCajaId, @FechaTransferencia, 0);
                        """,
                        new
                        {
                            MovimientoCajaId = resultId.Value,
                            FechaTransferencia = fechaPagoTransferencia,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new PagoCajaResultResponse(resultId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

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
