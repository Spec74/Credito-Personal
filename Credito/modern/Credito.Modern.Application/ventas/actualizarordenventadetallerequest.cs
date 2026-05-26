namespace Credito.Modern.Application.Ventas;

public sealed record ActualizarOrdenVentaDetalleRequest(int OficinaId, int OrdenVentaDetId, decimal Descuento);
