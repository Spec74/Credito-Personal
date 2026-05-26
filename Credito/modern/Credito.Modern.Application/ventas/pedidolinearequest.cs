namespace Credito.Modern.Application.Ventas;

public sealed record PedidoLineaRequest(int ArticuloId, int Cantidad, decimal Descuento);
