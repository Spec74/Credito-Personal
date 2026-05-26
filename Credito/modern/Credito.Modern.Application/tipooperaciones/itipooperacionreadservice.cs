namespace Credito.Modern.Application.TipoOperaciones;

public interface ITipoOperacionReadService
{
    Task<List<TipoOperacionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
