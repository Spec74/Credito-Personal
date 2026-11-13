namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Respuesta de <c>GET /api/v1/credito/monto-boveda-asignacion</c>.</summary>
/// <param name="Monto">SaldoFinal de la bóveda usada para asignar caja.</param>
/// <param name="EsBovedaTemporal">
/// True si el actor tiene rol ENCARGADO en la oficina (débito de temporal);
/// false = bóveda principal.
/// </param>
public sealed record MontoBovedaAsignacionResponse(decimal Monto, bool EsBovedaTemporal);
