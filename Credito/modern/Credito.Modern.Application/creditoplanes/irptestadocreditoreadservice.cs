namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptEstadoCreditoReadService
{
    Task<RptEstadoCreditoInformeDto?> ObtenerAsync(int creditoId, CancellationToken cancellationToken = default);
}
