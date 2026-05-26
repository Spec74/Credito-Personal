namespace Credito.Modern.Application.Ventas;

public interface IOrdenVentaOficinaReadService
{
    Task<int?> GetOficinaIdByOrdenVentaIdAsync(int ordenVentaId, CancellationToken cancellationToken = default);
}
