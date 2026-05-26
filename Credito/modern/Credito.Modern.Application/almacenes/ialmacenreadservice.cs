using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.Almacenes;

public interface IAlmacenReadService
{
    /// <summary>Almacenes activos; si <paramref name="oficinaId"/> es ≥ 1, filtra por oficina.</summary>
    Task<List<AlmacenListItemDto>> GetActivosAsync(int? oficinaId, CancellationToken cancellationToken = default);

    Task<List<AlmacenAdminListItemDto>> ListGestionAsync(
        int? oficinaId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default);
}