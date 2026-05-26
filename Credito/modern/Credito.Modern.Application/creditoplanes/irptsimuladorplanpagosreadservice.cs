namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptSimuladorPlanPagosReadService
{
    Task<RptSimuladorPlanPagosInformeDto?> GenerarAsync(
        RptSimuladorPlanPagosQuery query,
        CancellationToken cancellationToken = default);
}
