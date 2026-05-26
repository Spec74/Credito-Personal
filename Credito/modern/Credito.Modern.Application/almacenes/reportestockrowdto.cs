namespace Credito.Modern.Application.Almacenes;

/// <summary>Filas devueltas por <c>ALMACEN.usp_ReporteStock</c>.</summary>
public sealed record ReporteStockRowDto(
    long? Nro,
    string? TipoArticulo,
    int ArticuloId,
    string? Articulo,
    int Stock,
    string? Series);
