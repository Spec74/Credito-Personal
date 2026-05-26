namespace Credito.Modern.Application.Ventas;

public interface IOrdenVentaDetScopeReadService
{
    Task<OrdenVentaDetScopeDto?> GetScopeByDetIdAsync(
        int ordenVentaDetId,
        CancellationToken cancellationToken = default);
}
