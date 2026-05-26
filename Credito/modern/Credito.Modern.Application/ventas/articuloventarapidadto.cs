namespace Credito.Modern.Application.Ventas;

/// <summary>Paridad JSON de <c>VentaRapidaController.ObtenerArticulo</c>.</summary>
public sealed record ArticuloVentaRapidaDto(
    int ArticuloId,
    string CodArticulo,
    string Denominacion,
    decimal? PrecioVenta,
    int Stock);
