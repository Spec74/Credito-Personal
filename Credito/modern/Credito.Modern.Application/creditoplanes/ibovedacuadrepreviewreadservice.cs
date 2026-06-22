namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaCuadrePreviewReadService
{
    Task<BovedaCuadrePreviewDto?> ObtenerAsync(
        int oficinaId,
        int? bovedaId = null,
        CancellationToken cancellationToken = default);
}
