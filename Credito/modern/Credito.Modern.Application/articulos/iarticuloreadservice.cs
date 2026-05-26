namespace Credito.Modern.Application.Articulos;

public interface IArticuloReadService
{
    /// <summary>Artículos activos; filtros opcionales <paramref name="modeloId"/> y <paramref name="tipoArticuloId"/> (≥ 1).</summary>
    Task<List<ArticuloListItemDto>> GetActivosAsync(int? modeloId, int? tipoArticuloId, CancellationToken cancellationToken = default);

    Task<List<ArticuloGestionListItemDto>> ListGestionAsync(
        int? modeloId,
        int? tipoArticuloId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default);

    Task<ArticuloDetalleDto?> GetDetalleAsync(int articuloId, CancellationToken cancellationToken = default);

    Task<List<ArticuloBuscarItemDto>> BuscarSelectAsync(
        string term,
        bool soloActivos,
        CancellationToken cancellationToken = default);

    Task<string?> ObtenerImagenCsvAsync(int articuloId, CancellationToken cancellationToken = default);
}
