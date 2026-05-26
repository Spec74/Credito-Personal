namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/ingreso-egreso-boveda</c> (paridad <c>BovedaController.IngresoEgreso</c>).</summary>
public sealed record IngresoEgresoBovedaRequest(
    int OficinaId,
    decimal Importe,
    string Descripcion,
    int TipoOperacionId,
    short TipoPagoId);
