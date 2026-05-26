namespace Credito.Modern.Application.CreditoPlanes;

public interface IResumenCuentaBovedaReadService
{
    Task<string?> ObtenerPrimerTextoAsync(int bovedaId, CancellationToken cancellationToken = default);
}
