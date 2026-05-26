namespace Credito.Modern.Application.CreditoPlanes;

public interface ICompletarImpagosWriteService
{
    /// <summary>Ejecuta <c>CREDITO.usp_CompletarImpagos</c> dentro de una transacción SQL.</summary>
    Task<CompletarImpagosResponse> EjecutarAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default);
}
