namespace Credito.Modern.Application.TipoArticulos;

public sealed record TipoArticuloListItemDto(
    int TipoArticuloId,
    string Denominacion,
    string? Descripcion,
    bool? IndTieneCodigo,
    bool Estado,
    bool? IndMovimientoAlmacen);
