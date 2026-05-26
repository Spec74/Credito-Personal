namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/completar-impagos</c>.</summary>
public sealed record CompletarImpagosRequest(int OficinaId, int CajaDiarioId);
