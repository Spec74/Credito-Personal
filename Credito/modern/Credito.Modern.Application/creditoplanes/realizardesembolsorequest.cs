namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/realizar-desembolso</c>.</summary>
public sealed record RealizarDesembolsoRequest(int OficinaId, int CajaDiarioId, int CreditoId);
