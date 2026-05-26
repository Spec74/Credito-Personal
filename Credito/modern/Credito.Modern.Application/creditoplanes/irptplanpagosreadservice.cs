namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptPlanPagosReadService
{
    Task<IReadOnlyList<RptPlanPagosRowDto>> ListarAsync(int creditoId, CancellationToken cancellationToken = default);
}
