namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Respuesta de <c>GET /api/v1/credito/monto-boveda-asignacion</c>.</summary>
/// <param name="Monto">Efectivo disponible (TipoPagoId=1) en la bóveda fuente.</param>
/// <param name="EsBovedaTemporal">
/// True si hay bóveda temporal abierta en la oficina (débito de temporal);
/// false = bóveda principal. Independiente del rol ENCARGADO (gerente de sede).
/// </param>
public sealed record MontoBovedaAsignacionResponse(decimal Monto, bool EsBovedaTemporal);
