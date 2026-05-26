using Credito.Modern.Application.Maestros;

using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.Modelos;

public interface IModeloReadService
{
    /// <summary>Modelos activos; si <paramref name="marcaId"/> es ≥ 1, filtra por marca.</summary>
    Task<List<ModeloListItemDto>> GetActivosAsync(int? marcaId, CancellationToken cancellationToken = default);

    Task<List<ModeloAdminListItemDto>> ListAsync(
        int? marcaId,
        bool incluirInactivos,
        CancellationToken cancellationToken = default);
}
