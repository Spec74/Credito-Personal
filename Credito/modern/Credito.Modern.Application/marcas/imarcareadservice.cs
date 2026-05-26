namespace Credito.Modern.Application.Marcas;

public interface IMarcaReadService
{
    Task<List<MarcaListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default);

    Task<List<MarcaListItemDto>> ListAsync(
        bool incluirInactivos,
        CancellationToken cancellationToken = default);
}
