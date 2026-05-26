namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/pagar-cuotas-cancelacion</c>.</summary>
public sealed record PagarCuotasCancelacionRequest(int OficinaId, int CajaDiarioId, int CreditoId);
