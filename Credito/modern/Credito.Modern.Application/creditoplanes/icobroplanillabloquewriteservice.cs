namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Paridad <c>CreditoController.CobrarPlanillaBloque</c>:
/// pagos libres con monto &gt; 0 + <c>usp_CompletarImpagos</c> en una sola transacción.
/// </summary>
public interface ICobroPlanillaBloqueWriteService
{
    Task<CobrarPlanillaBloqueResponse> EjecutarAsync(
        int cajaDiarioId,
        int usuarioId,
        IReadOnlyList<PagoPlanillaItemDto> planilla,
        CancellationToken cancellationToken = default);
}