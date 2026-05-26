namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaAbiertaReadService
{
    /// <summary>
    /// Bóveda abierta de la oficina (<c>IndCierre = 0</c>), misma prioridad que escritura:
    /// <c>ORDER BY IndTemporal, BovedaId</c>.
    /// </summary>
    Task<BovedaAbiertaDto?> ObtenerAbiertaPorOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
