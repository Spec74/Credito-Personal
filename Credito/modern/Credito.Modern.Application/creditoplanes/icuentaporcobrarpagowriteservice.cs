namespace Credito.Modern.Application.CreditoPlanes;

public interface ICuentaPorCobrarPagoWriteService
{
    Task<PagoCajaResultResponse> PagarAsync(
        int ordenVentaId,
        int cuentaxCobrarId,
        int cajaDiarioId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
