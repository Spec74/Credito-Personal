namespace Credito.Modern.Application.ListaPrecios;

public interface IListaPrecioReadService
{
    /// <summary>Precios activos; si <paramref name="articuloId"/> es ≥ 1, filtra por artículo.</summary>
    Task<List<ListaPrecioListItemDto>> GetActivosAsync(int? articuloId, CancellationToken cancellationToken = default);

    Task<List<ListaPrecioListItemDto>> ListAsync(
        int? articuloId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default);
}
