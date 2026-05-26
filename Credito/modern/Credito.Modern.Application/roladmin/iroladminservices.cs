using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.RolAdmin;

public interface IRolAdminReadService
{
    Task<List<RolGestionListItemDto>> ListGestionAsync(
        bool incluirInactivos,
        CancellationToken cancellationToken = default);

    Task<RolMenusDetalleDto?> GetMenusDetalleAsync(int rolId, CancellationToken cancellationToken = default);
}

public interface IRolAdminWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarRolRequest request, CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> ActivarAsync(int rolId, CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> AsignarMenusAsync(
        int rolId,
        IReadOnlyList<int> menuIds,
        CancellationToken cancellationToken = default);
}
