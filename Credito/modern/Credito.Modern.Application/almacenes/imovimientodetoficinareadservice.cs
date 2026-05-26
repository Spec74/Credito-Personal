namespace Credito.Modern.Application.Almacenes;

public interface IMovimientoDetOficinaReadService
{
    Task<int?> GetOficinaIdByMovimientoDetIdAsync(int movimientoDetId, CancellationToken cancellationToken = default);
}
