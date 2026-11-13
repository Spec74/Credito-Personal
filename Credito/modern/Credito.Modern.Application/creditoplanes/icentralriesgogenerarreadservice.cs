namespace Credito.Modern.Application.CreditoPlanes;

public interface ICentralRiesgoGenerarReadService
{
    /// <param name="regenerar">
    /// Si true, borra el snapshot de la sede/periodo y lo recalcula
    /// (<c>usp_CentralRiesgoGenerar @Regenerar=1</c>).
    /// </param>
    Task<IReadOnlyList<CentralRiesgoGenerarRowDto>> ListarAsync(
        int oficinaId,
        int anio,
        int mes,
        bool regenerar = false,
        CancellationToken cancellationToken = default);
}
