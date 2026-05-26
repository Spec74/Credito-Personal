namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/prorrogar-credito</c>.</summary>
public sealed record ProrrogarCreditoRequest(int OficinaId, int CreditoId, int Dias);
