namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/crear-solicitud-credito</c>.</summary>
public sealed record CrearSolicitudCreditoRequest(int OficinaId, int PersonaId);
