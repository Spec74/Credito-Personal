namespace Credito.Modern.Application.Ubigeo;

public interface IDistritoReadService
{
    /// <summary>Distritos; si <paramref name="provinciaId"/> es ≥ 1, filtra por provincia.</summary>
    Task<List<DistritoListItemDto>> GetAsync(int? provinciaId, CancellationToken cancellationToken = default);
}
