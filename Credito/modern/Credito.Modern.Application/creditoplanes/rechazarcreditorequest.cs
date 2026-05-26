namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/rechazar-credito</c>.</summary>
public sealed record RechazarCreditoRequest(int OficinaId, int CreditoId);
