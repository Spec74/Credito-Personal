namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoScopeReadService
{
    Task<CreditoScopeDto?> GetScopeAsync(int creditoId, CancellationToken cancellationToken = default);
}
