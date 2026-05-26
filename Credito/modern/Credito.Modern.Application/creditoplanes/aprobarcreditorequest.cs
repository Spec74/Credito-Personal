namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cuerpo de <c>POST /api/v1/credito/aprobar-credito</c>.
/// <c>opcion</c> 0 = primera aprobación (<c>AprobarCredito1ra</c>); 1 = segunda (<c>AprobarCredito</c>).
/// </summary>
public sealed record AprobarCreditoRequest(int OficinaId, int CreditoId, int Opcion);
