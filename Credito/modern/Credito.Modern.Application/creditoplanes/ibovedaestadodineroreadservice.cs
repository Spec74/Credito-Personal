namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaEstadoDineroReadService
{
    Task<BovedaEstadoDineroDto> ObtenerAsync(
        int oficinaId,
        decimal saldoBovedaAbierta,
        CancellationToken cancellationToken = default);
}
