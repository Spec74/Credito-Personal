using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.Articulos;

public sealed record GuardarArticuloRequest(
    int ArticuloId,
    int ModeloId,
    int TipoArticuloId,
    string CodArticulo,
    string Denominacion,
    string? Descripcion,
    decimal Monto,
    decimal Descuento,
    bool IndPerecible,
    bool IndImportado,
    bool IndCanjeable,
    bool Estado);

public sealed record ArticuloDetalleDto(
    int ArticuloId,
    int? ModeloId,
    int? TipoArticuloId,
    string CodArticulo,
    string Denominacion,
    string? Descripcion,
    bool? IndPerecible,
    bool? IndImportado,
    bool? IndCanjeable,
    bool Estado,
    decimal? Monto,
    decimal? Descuento,
    int? ListaPrecioId);

public sealed record ArticuloGestionListItemDto(
    int ArticuloId,
    int? ModeloId,
    string? ModeloDenominacion,
    int? TipoArticuloId,
    string? TipoArticuloDenominacion,
    string CodArticulo,
    string Denominacion,
    string? Descripcion,
    bool? IndPerecible,
    bool? IndImportado,
    bool? IndCanjeable,
    bool Estado,
    decimal? Monto,
    decimal? Descuento);

public sealed record ArticuloBuscarItemDto(int ArticuloId, string Denominacion);

public sealed record ArticuloImagenesResponse(string[] Archivos);
