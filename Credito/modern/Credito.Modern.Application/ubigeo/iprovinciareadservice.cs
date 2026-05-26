namespace Credito.Modern.Application.Ubigeo;

public interface IProvinciaReadService
{
    /// <summary>Provincias; si <paramref name="departamentoId"/> es ≥ 1, filtra por departamento.</summary>
    Task<List<ProvinciaListItemDto>> GetAsync(int? departamentoId, CancellationToken cancellationToken = default);
}
