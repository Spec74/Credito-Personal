namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/reprogramar-credito</c>.</summary>
public sealed record ReprogramarCreditoRequest(int OficinaId, int CreditoId);
