namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/recalcular-caja-diario</c>.</summary>
public sealed record RecalcularCajaDiarioRequest(int OficinaId, int CajaDiarioId);
