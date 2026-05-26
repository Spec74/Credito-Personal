namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/pagar-cuota-importe-libre</c>.</summary>
public sealed record PagarCuotaPagoLibreRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    decimal ImporteRecibido,
    int TipoPagoId = 1,
    string? FechaPagoTransferencia = null);
