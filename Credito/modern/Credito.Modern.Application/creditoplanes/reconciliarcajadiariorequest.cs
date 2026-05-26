namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/reconciliar-caja-diario</c>.</summary>
public sealed record ReconciliarCajaDiarioRequest(int OficinaId, int CajaDiarioId);
