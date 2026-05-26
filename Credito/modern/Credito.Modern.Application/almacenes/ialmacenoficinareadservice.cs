namespace Credito.Modern.Application.Almacenes;

/// <summary>Resuelve la oficina de un almacén para autorizar lecturas por <c>AlmacenId</c>.</summary>
public interface IAlmacenOficinaReadService
{
    /// <summary><c>OficinaId</c> de <c>ALMACEN.Almacen</c>, o <c>null</c> si no existe la fila.</summary>
    Task<int?> GetOficinaIdByAlmacenIdAsync(int almacenId, CancellationToken cancellationToken = default);
}
