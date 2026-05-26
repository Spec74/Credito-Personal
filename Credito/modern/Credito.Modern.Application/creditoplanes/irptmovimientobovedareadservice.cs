namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptMovimientoBovedaReadService
{
    Task<IReadOnlyList<RptMovimientoBovedaRowDto>> ListarAsync(
        int bovedaId,
        CancellationToken cancellationToken = default);
}
