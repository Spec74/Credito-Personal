namespace Credito.Modern.Application.Ventas;

public sealed record OrdenVentaEnvioDto(
    int OrdenVentaId,
    int OficinaId,
    int PersonaId,
    decimal TotalNeto,
    string Estado,
    string TipoVenta);
