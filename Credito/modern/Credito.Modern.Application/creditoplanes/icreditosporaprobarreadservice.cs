namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditosPorAprobarReadService
{
    Task<CreditosPorAprobarListResponse> ListarAsync(
        string? buscar,
        int page,
        int pageSize,
        string sortField,
        string sortOrder,
        CancellationToken cancellationToken = default);
}
