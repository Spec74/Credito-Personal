namespace Credito.Modern.Application.Ventas;

public sealed record AgregarOrdenVentaDetalleRequest(int OficinaId, int OrdenVentaId, string NumeroSerie);
