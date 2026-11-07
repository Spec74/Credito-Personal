using System.Data;
using System.Globalization;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Validation;
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

    private static readonly IReadOnlyList<CobroPlanillaTicketDto> SinTickets =
        Array.Empty<CobroPlanillaTicketDto>();

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CobrarPlanillaBloqueResponse> EjecutarAsync(
        int cajaDiarioId,
        int usuarioId,
        IReadOnlyList<PagoPlanillaItemDto> planilla,
        bool completarImpagos = false,
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var esCentral = await TipoPagoOficinaSql
            .EsCentralPorCajaDiarioAsync(connection, cajaDiarioId, cancellationToken)
            .ConfigureAwait(false);

        var items = new List<PagoPlanillaItemDto>();
        foreach (var raw in planilla)
        {
            var monto = decimal.Round(raw.MontoPagar, 2, MidpointRounding.AwayFromZero);
            if (monto <= 0m)
            {
                continue;
            }

            var moneyErr = MoneyRules.RequirePositive(monto, $"crédito {raw.CreditoId} monto");
            if (moneyErr is not null)
            {
                return new CobrarPlanillaBloqueResponse(false, moneyErr, 0, 0, SinTickets);
            }

            if (raw.CreditoId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(planilla), "creditoId inválido en la planilla.");
            }

            var tipoErr = TipoPagoRules.RequireCobroPermitido(raw.TipoPagoId, esCentral);
            if (tipoErr is not null)
            {
                return new CobrarPlanillaBloqueResponse(
                    false,
                    $"Crédito {raw.CreditoId}: {tipoErr}",
                    0,
                    0,
                    SinTickets);
            }

            var fechaErr = TipoPagoRules.RequireDigitalFecha(
                raw.TipoPagoId,
                raw.FechaHoraTrans,
                $"crédito {raw.CreditoId} fecha/hora");
            if (fechaErr is not null)
            {
                return new CobrarPlanillaBloqueResponse(false, fechaErr, 0, 0, SinTickets);
            }

            items.Add(raw with { MontoPagar = monto });
        }

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
                    0,
                    SinTickets);
            }

            // Multisucursal: cada crédito de la planilla debe ser de la oficina de la caja.
            if (items.Count > 0)
            {
                var oficinaCaja = await connection.ExecuteScalarAsync<int?>(
                    new CommandDefinition(
                        """
                        SELECT cj.OficinaId
                        FROM CREDITO.CajaDiario cd
                        INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
                        WHERE cd.CajaDiarioId = @CajaDiarioId;
                        """,
                        new { CajaDiarioId = cajaDiarioId },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                if (oficinaCaja is null or < 1)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return new CobrarPlanillaBloqueResponse(
                        false,
                        "No se pudo resolver la oficina de la caja diario.",
                        0,
                        0,
                        SinTickets);
                }

                var creditoIds = items.Select(i => i.CreditoId).Distinct().ToArray();
                var oficinasCredito = (await connection.QueryAsync<(int CreditoId, int OficinaId)>(
                    new CommandDefinition(
                        """
                        SELECT CreditoId, OficinaId
                        FROM CREDITO.Credito
                        WHERE CreditoId IN @CreditoIds;
                        """,
                        new { CreditoIds = creditoIds },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false))
                    .ToDictionary(x => x.CreditoId, x => x.OficinaId);

                foreach (var creditoId in creditoIds)
                {
                    if (!oficinasCredito.TryGetValue(creditoId, out var oid))
                    {
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return new CobrarPlanillaBloqueResponse(
                            false,
                            $"Crédito {creditoId}: no existe.",
                            0,
                            0,
                            SinTickets);
                    }

                    if (oid != oficinaCaja.Value)
                    {
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return new CobrarPlanillaBloqueResponse(
                            false,
                            $"Crédito {creditoId}: no pertenece a la oficina de la caja (multisucursal).",
                            0,
                            0,
                            SinTickets);
                    }
                }
            }

            var tickets = new List<CobroPlanillaTicketDto>(items.Count);
            var pagos = 0;
            foreach (var item in items)
            {
                string fechaSegura;
                try
                {
                    fechaSegura = NormalizarFechaTransferencia(
                        item.FechaHoraTrans,
                        TipoPagoRules.IsDigital(item.TipoPagoId));
                }
                catch (ArgumentException ex)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return new CobrarPlanillaBloqueResponse(
                        false,
                        $"Crédito {item.CreditoId}: {ex.Message}",
                        pagos,
                        0,
                        SinTickets);
                }

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
                        0,
                        SinTickets);
                }

                tickets.Add(new CobroPlanillaTicketDto(item.CreditoId, resultId.Value));
                pagos++;
            }

            var impagos = 0;
            if (completarImpagos)
            {
                // Solo flujo legacy / cierre explícito de planilla (no Cobro PWA moderno).
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "CREDITO.usp_CompletarImpagos",
                        new { CajaDiarioId = cajaDiarioId },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
                impagos = 1;
            }

            if (pagos < 1 && !completarImpagos)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new CobrarPlanillaBloqueResponse(
                    false,
                    "No hay cobros con monto para registrar. Tipé montos o use Completar impagos en Caja Diario.",
                    0,
                    0,
                    SinTickets);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CobrarPlanillaBloqueResponse(
                true,
                completarImpagos
                    ? (pagos > 0
                        ? "Planilla e impagos procesados con éxito."
                        : "Planilla cerrada: todos los clientes quedaron como impagos.")
                    : $"Se registraron {pagos} cobro(s) correctamente.",
                pagos,
                impagos,
                tickets);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Digital: fecha obligatoria y parseable (no inventa "ahora").
    /// Efectivo: cadena vacía.
    /// </summary>
    private static string NormalizarFechaTransferencia(string? raw, bool esDigital)
    {
        if (!esDigital)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("Fecha/hora del voucher digital es obligatoria.");
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

        throw new ArgumentException(
            $"Fecha/hora del voucher no es válida: «{raw.Trim()}».");
    }
}
