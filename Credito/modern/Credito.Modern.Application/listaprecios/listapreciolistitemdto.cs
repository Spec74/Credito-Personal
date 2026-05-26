namespace Credito.Modern.Application.ListaPrecios;

public sealed record ListaPrecioListItemDto(
    int ListaPrecioId,
    int? ArticuloId,
    decimal? Monto,
    decimal? Descuento,
    bool Estado,
    int? Puntos,
    int? PuntosCanje);
