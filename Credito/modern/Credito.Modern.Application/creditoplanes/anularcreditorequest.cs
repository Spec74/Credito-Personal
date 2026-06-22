namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/anular-credito</c> (paridad <c>AnularCredito</c> MVC).</summary>
public sealed record AnularCreditoRequest(int OficinaId, int CreditoId, string Observacion, string ClaveAutorizacion);
