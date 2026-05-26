namespace Credito.Modern.Application.Almacenes;

public interface IMovimientoOficinaReadService
{
    Task<int?> GetOficinaIdByMovimientoIdAsync(int movimientoId, CancellationToken cancellationToken = default);
}
