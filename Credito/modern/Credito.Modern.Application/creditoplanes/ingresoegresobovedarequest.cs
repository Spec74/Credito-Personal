namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/ingreso-egreso-boveda</c> (paridad <c>BovedaController.IngresoEgreso</c>).</summary>
/// <param name="FechaPagoTransferencia">Obligatoria si el medio es digital (Yape/Plin/banco), igual que cobros.</param>
public sealed record IngresoEgresoBovedaRequest(
    int OficinaId,
    decimal Importe,
    string Descripcion,
    int TipoOperacionId,
    short TipoPagoId,
    string? FechaPagoTransferencia = null);
