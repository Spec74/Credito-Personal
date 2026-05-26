namespace Credito.Modern.Application.CreditoPlanes;

public interface ICompletarImpagosValidacionReadService
{
    /// <summary>Ejecuta <c>CREDITO.usp_CompletarImpagosValidacion</c> (<c>CajaDiarioId</c>).</summary>
    Task<CompletarImpagosValidacionResponse> ValidarAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default);
}
