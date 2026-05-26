namespace Credito.Modern.Application.CreditoPlanes;

public interface ISimuladorCreditoReadService
{
    Task<IReadOnlyList<SimuladorCreditoCuotaDto>> SimularAsync(
        decimal monto,
        string formaPago,
        int nroCuotas,
        decimal interesMensual,
        DateTime fechaPrimerPago,
        decimal gastosAdm,
        CancellationToken cancellationToken = default);
}
