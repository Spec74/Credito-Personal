namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/cerrar-caja-diario</c>.</summary>
public sealed record CerrarCajaDiarioRequest(int OficinaId, int CajaDiarioId);
