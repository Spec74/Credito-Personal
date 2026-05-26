namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptMovimientoCreditoReadService
{
    Task<List<RptMovimientoCreditoRowDto>> ListarPorCreditoAsync(
        int creditoId,
        CancellationToken cancellationToken = default);
}
