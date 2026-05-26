namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaDiarioOperacionWriteService
{
    Task<CajaDiarioOperacionResponse> ReconciliarCajaDiarioAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default);

    Task<CajaDiarioOperacionResponse> RecalcularCajaDiarioAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default);

    Task<CajaDiarioOperacionResponse> CerrarCajasDiariosAsync(
        int usuarioCierreId,
        int oficinaId,
        decimal sobrante,
        CancellationToken cancellationToken = default);
}
