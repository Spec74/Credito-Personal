namespace Credito.Modern.Application.TipoArticulos;

public interface ITipoArticuloReadService
{
    Task<List<TipoArticuloListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default);

    Task<List<TipoArticuloListItemDto>> ListAsync(
        bool incluirInactivos,
        CancellationToken cancellationToken = default);
}
