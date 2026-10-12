namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cobro de planilla/cola: pagos libres con monto &gt; 0 en una sola transacción.
/// <c>completarImpagos</c> es opcional (solo flujo legacy MVC / cierre de planilla).
/// </summary>
public interface ICobroPlanillaBloqueWriteService
{
    Task<CobrarPlanillaBloqueResponse> EjecutarAsync(
        int cajaDiarioId,
        int usuarioId,
        IReadOnlyList<PagoPlanillaItemDto> planilla,
        bool completarImpagos = false,
        CancellationToken cancellationToken = default);
}