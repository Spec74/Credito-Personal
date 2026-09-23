using System.Data;
using System.Globalization;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// Orquesta la planilla de cobranza diaria en una sola transacción SQL
/// (paridad <c>TransactionScope</c> del MVC).
/// </summary>
public sealed class CobroPlanillaBloqueWriteService(IOptions<SqlDatabaseOptions> options)
    : ICobroPlanillaBloqueWriteService
{
    private static readonly string[] FormatosFecha =
    [
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy HH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
    ];

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CobrarPlanillaBloqueResponse> EjecutarAsync(
        int cajaDiarioId,
        int usuarioId,
        IReadOnlyList<PagoPlanillaItemDto> planilla,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (cajaDiarioId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId y usuarioId deben ser >= 1.");
        }

        ArgumentNullException.ThrowIfNull(planilla);

        var items = planilla.Where(x => x.MontoPagar > 0).ToList();
        if (items.Count == 0)
        {
            return new CobrarPlanillaBloqueResponse(
                false,
                "La planilla enviada está vacía o no tiene montos mayores a cero.",
                0,
                0);
        }

        foreach (var item in items)
        {
            if (item.CreditoId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(planilla), "creditoId inválido en la planilla.");
            }

            if (item.TipoPagoId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(planilla), "tipoPagoId inválido en la planilla.");
            }
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var cajaOk = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM CREDITO.CajaDiario
                        WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = CAST(0 AS bit)
                    ) THEN 1 ELSE 0 END;
                    """,
                    new { CajaDiarioId = cajaDiarioId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajaOk != 1)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new CobrarPlanillaBloqueResponse(
                    false,
                    "La caja diario no existe o está cerrada.",
                    0,
                    0);
            }

            var pagos = 0;
            foreach (var item in items)
            {
                var fechaSegura = NormalizarFechaTransferencia(item.FechaHoraTrans);
                var resultId = await connection.QueryFirstOrDefaultAsync<int?>(
                    new CommandDefinition(
                        "CREDITO.usp_PagarCuotaPagoLibre",
                        new
                        {
                            CajaDiarioId = cajaDiarioId,
                            CreditoId = item.CreditoId,
                            ImporteRecibido = item.MontoPagar,
                            UsuarioId = usuarioId,
                            TipoPagoId = item.TipoPagoId,
                            FechaPagoTransferencia = fechaSegura,
                        },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                if (resultId is null or < 1)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return new CobrarPlanillaBloqueResponse(
                        false,
                        $"Proceso abortado por seguridad: error al procesar el pago del crédito {item.CreditoId}.",
                        pagos,
                        0);
                }

                pagos++;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "CREDITO.usp_CompletarImpagos",
                    new { CajaDiarioId = cajaDiarioId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CobrarPlanillaBloqueResponse(
                true,
                "Planilla e impagos procesados con éxito.",
                pagos,
                1);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static string NormalizarFechaTransferencia(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParseExact(
                raw.Trim(),
                FormatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed)
            || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            || DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
        {
            return parsed.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        return DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
