namespace Credito.Modern.Application.Articulos;

/// <summary>Listado de artículo; sin <c>Imagen</c> (varchar(max)) para evitar respuestas pesadas.</summary>
public sealed record ArticuloListItemDto(
    int ArticuloId,
    int? ModeloId,
    int? TipoArticuloId,
    string CodArticulo,
    string Denominacion,
    string? Descripcion,
    bool? IndPerecible,
    bool? IndImportado,
    bool? IndCanjeable,
    bool Estado);
