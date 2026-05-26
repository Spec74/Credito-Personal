namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resuelve la oficina de una bóveda para autorizar lecturas por <c>BovedaId</c>.</summary>
public interface IBovedaOficinaReadService
{
    /// <summary><c>OficinaId</c> de <c>CREDITO.Boveda</c>, o <c>null</c> si no existe la fila.</summary>
    Task<int?> GetOficinaIdByBovedaIdAsync(int bovedaId, CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>BovedaController.ExisteBovedaTemporal</c>.</summary>
    Task<bool> ExisteBovedaTemporalAbiertaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
