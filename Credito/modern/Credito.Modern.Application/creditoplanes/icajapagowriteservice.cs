namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaPagoWriteService
{
    Task<PagoCajaResultResponse> PagarCuotasAsync(
        int cajaDiarioId,
        int creditoId,
        string listaPlanPagoId,
        decimal importeRecibido,
        int usuarioId,
        DateTime fechaPago,
        int tipoPagoId,
        string fechaPagoTransferencia,
        CancellationToken cancellationToken = default);

    Task<PagoCajaResultResponse> PagarCuotaPagoLibreAsync(
        int cajaDiarioId,
        int creditoId,
        decimal importeRecibido,
        int usuarioId,
        int tipoPagoId,
        string fechaPagoTransferencia,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cobro mixto: una línea por medio (Yape/BCP/Interbank/Nación/efectivo…). Misma caja/crédito; una transacción.
    /// </summary>
    Task<PagoCajaMixtoResultResponse> PagarCuotaPagoLibreMixtoAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        IReadOnlyList<PagoMedioLineaDto> medios,
        CancellationToken cancellationToken = default);

    Task<PagoCajaResultResponse> PagarCuotasCancelacionAsync(
        int cajaDiarioId,
        int creditoId,
        int usuarioId,
        DateTime fechaPago,
        CancellationToken cancellationToken = default);
}
