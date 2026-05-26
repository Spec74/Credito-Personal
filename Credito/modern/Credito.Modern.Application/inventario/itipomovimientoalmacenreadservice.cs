namespace Credito.Modern.Application.Inventario;

public interface ITipoMovimientoAlmacenReadService
{
    Task<List<TipoMovimientoAlmacenListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default);
}
