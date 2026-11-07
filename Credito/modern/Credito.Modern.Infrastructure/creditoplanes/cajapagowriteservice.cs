using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Validation;
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

    public async Task<PagoCajaMixtoResultResponse> PagarCuotaPagoLibreMixtoAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        IReadOnlyList<PagoMedioLineaDto> medios,
        CancellationToken cancellationToken = default)
    {
        EnsureConnectionConfigured();
        if (medios is null || medios.Count < 2)
        {
            throw new ArgumentException("El cobro mixto requiere al menos 2 medios.", nameof(medios));
        }

        if (medios.Count > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(medios), "Máximo 8 medios por cobro mixto.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var esCentral = await TipoPagoOficinaSql
            .EsCentralPorCajaDiarioAsync(connection, cajaDiarioId, cancellationToken)
            .ConfigureAwait(false);

        var normalizados = new List<PagoMedioLineaDto>(medios.Count);
        decimal total = 0m;
        var i = 0;
        foreach (var linea in medios)
        {
            i++;
            var importe = decimal.Round(linea.Importe, 2, MidpointRounding.AwayFromZero);
            var err = MoneyRules.RequirePositive(importe, $"medios[{i}].importe");
            if (err is not null)
            {
                throw new ArgumentException(err, nameof(medios));
            }

            var tipoErr = TipoPagoRules.RequireCobroPermitido(
                linea.TipoPagoId,
                esCentral,
                $"medios[{i}].tipoPagoId");
            if (tipoErr is not null)
            {
                throw new ArgumentException(tipoErr, nameof(medios));
            }

            var fechaErr = TipoPagoRules.RequireDigitalFecha(
                linea.TipoPagoId,
                linea.FechaPagoTransferencia,
                $"medios[{i}].fechaPagoTransferencia");
            if (fechaErr is not null)
            {
                throw new ArgumentException(fechaErr, nameof(medios));
            }

            total = decimal.Round(total + importe, 2, MidpointRounding.AwayFromZero);
            normalizados.Add(linea with { Importe = importe });
        }

        var totalErr = MoneyRules.RequirePositive(total, "suma de medios");
        if (totalErr is not null)
        {
            throw new ArgumentException(totalErr, nameof(medios));
        }

        var sumaLineas = normalizados.Sum(x => x.Importe);
        if (sumaLineas != total)
        {
            throw new InvalidOperationException(
                "Inconsistencia de céntimos en cobro mixto: revise los importes.");
        }

        var ids = new List<int>(normalizados.Count);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            foreach (var linea in normalizados)
            {
                var command = new CommandDefinition(
                    "CREDITO.usp_PagarCuotaPagoLibre",
                    new
                    {
                        CajaDiarioId = cajaDiarioId,
                        CreditoId = creditoId,
                        ImporteRecibido = linea.Importe,
                        UsuarioId = usuarioId,
                        TipoPagoId = linea.TipoPagoId,
                        FechaPagoTransferencia = linea.FechaPagoTransferencia ?? string.Empty,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken);
                var resultId = await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
                if (resultId is null or < 1)
                {
                    throw new InvalidOperationException(
                        "El cobro mixto se abortó: un medio no generó movimiento (sin cambios parciales).");
                }

                ids.Add(resultId.Value);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new PagoCajaMixtoResultResponse(ids[^1], ids);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public Task<PagoCajaResultResponse> PagarCuotasCancelacionAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        DateTime fechaPago,
        int tipoPagoId = 1,
        string fechaPagoTransferencia = "",
        CancellationToken cancellationToken = default) =>
        ExecutePagarCuotasWithExtensionAsync(
            "CREDITO.usp_PagarCuotasCancelacion",
            new
            {
                CajaDiarioId = cajaDiarioId,
                CreditoId = creditoId,
                UsuarioId = usuarioId,
                FechaPago = fechaPago.Date,
                TipoPagoId = tipoPagoId < 1 ? 1 : tipoPagoId,
                FechaPagoTransferencia = fechaPagoTransferencia ?? string.Empty,
            },
            tipoPagoId < 1 ? 1 : tipoPagoId,
            fechaPagoTransferencia ?? string.Empty,
            cancellationToken);

    /// <summary>
    /// Paridad operativa: si el medio es digital/transferencia (TipoPagoId &gt; 1),
    /// el movimiento debe tener Extension
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

            if (TipoPagoRules.IsDigital(tipoPagoId) && resultId is > 0)
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
