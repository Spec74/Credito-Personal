namespace Credito.Modern.Application.Productos;

public interface IProductoReadService
{
    Task<List<ProductoListItemDto>> GetActivosAsync(CancellationToken cancellationToken = default);

    Task<ProductoListItemDto?> GetActivoByIdAsync(
        int productoId,
        CancellationToken cancellationToken = default);
}
