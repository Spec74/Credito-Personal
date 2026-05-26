namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptCajasAsignadasReadService
{
    Task<List<RptCajasAsignadasRowDto>> ListarPorOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
