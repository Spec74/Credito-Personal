namespace Credito.Modern.Application.Ubigeo;

public interface IDepartamentoReadService
{
    Task<List<DepartamentoListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
