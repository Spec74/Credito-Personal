namespace Credito.Modern.Application.Ventas;

public sealed record CrearOrdenVentaRequest(int OficinaId, int PersonaId, string? TipoVenta);

public sealed record CrearOrdenVentaResponse(int OrdenVentaId);
