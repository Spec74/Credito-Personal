namespace Credito.Modern.Application.Ventas;

public sealed record ArticuloCanjeListItemDto(
    int ListaPrecioId,
    int ArticuloId,
    string TipoArticulo,
    string ArticuloDesc,
    int? PuntosCanje,
    bool Estado);
