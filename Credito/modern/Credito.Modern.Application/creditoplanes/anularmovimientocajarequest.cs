namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/anular-movimiento-caja</c>.</summary>
public sealed record AnularMovimientoCajaRequest(int OficinaId, int MovimientoCajaId, string? Observacion);
