namespace Credito.Modern.Application.SerieArticulos;

public interface ISerieArticuloReadService
{
    /// <summary>
    /// Series en <c>ALMACEN.SerieArticulo</c> filtradas por almacén y artículo.
    /// Por defecto <paramref name="estadoId"/> = 2 (EN_ALMACEN, ver <c>Constante.SerieArticulo</c> en BL).
    /// </summary>
    Task<List<SerieArticuloListItemDto>> ListarPorAlmacenArticuloAsync(
        int almacenId,
        int articuloId,
        int estadoId,
        int limite,
        CancellationToken cancellationToken = default);
}
