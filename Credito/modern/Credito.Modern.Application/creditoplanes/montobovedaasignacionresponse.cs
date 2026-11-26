namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Respuesta de <c>GET /api/v1/credito/monto-boveda-asignacion</c>.</summary>
/// <param name="Monto">SaldoFinal de la bóveda usada para asignar caja.</param>
/// <param name="EsBovedaTemporal">
/// True si hay bóveda temporal abierta en la oficina (débito de temporal);
/// false = bóveda principal. Independiente del rol ENCARGADO (gerente de sede).
/// </param>
public sealed record MontoBovedaAsignacionResponse(decimal Monto, bool EsBovedaTemporal);
