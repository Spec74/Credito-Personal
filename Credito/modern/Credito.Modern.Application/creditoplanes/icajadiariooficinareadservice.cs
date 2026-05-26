namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resuelve la oficina de una caja diario (vía <c>CREDITO.Caja</c>) para autorizar lecturas por <c>CajaDiarioId</c>.</summary>
public interface ICajaDiarioOficinaReadService
{
    /// <summary><c>OficinaId</c> de la caja asociada, o <c>null</c> si no existe la caja diario.</summary>
    Task<int?> GetOficinaIdByCajaDiarioIdAsync(int cajaDiarioId, CancellationToken cancellationToken = default);
}
