namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoAnulacionReadService
{
    Task<ValidarAnularCreditoResponse> ValidarAnularAsync(
        int creditoId,
        CancellationToken cancellationToken = default);
}
