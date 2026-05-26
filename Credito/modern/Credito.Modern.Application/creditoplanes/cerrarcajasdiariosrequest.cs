namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/cerrar-cajas-diarios</c> (transferencia a bóveda con sobrante).</summary>
public sealed record CerrarCajasDiariosRequest(int OficinaId, decimal Sobrante = 0m);
