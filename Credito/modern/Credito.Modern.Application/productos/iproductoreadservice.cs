namespace Credito.Modern.Application.Productos;

public interface IProductoReadService
{
    Task<List<ProductoListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default);
}
