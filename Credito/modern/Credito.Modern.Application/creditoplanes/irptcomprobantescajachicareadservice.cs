namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptComprobantesCajaChicaReadService
{
    Task<IReadOnlyList<RptComprobantesCajaChicaRowDto>> ListarAsync(
        DateTime fechaIni,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);
}
