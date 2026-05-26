namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resuelve la oficina de un crédito para autorizar lecturas por <c>CreditoId</c>.</summary>
public interface ICreditoOficinaReadService
{
    /// <summary><c>OficinaId</c> de <c>CREDITO.Credito</c>, o <c>null</c> si no existe la fila.</summary>
    Task<int?> GetOficinaIdByCreditoIdAsync(int creditoId, CancellationToken cancellationToken = default);
}
