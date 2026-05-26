namespace Credito.Modern.Application.CreditoPlanes;

public interface IEstadoPlanPagoReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_EstadoPlanPago</c> para el crédito indicado.</summary>
    Task<List<EstadoPlanPagoCuotaDto>> ListarPorCreditoAsync(int creditoId, CancellationToken cancellationToken = default);
}
