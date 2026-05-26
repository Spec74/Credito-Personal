namespace Credito.Modern.Application.CreditoPlanes;

public interface ICuentaPorCobrarPagoReadService
{
    Task<IReadOnlyList<CuentaPorCobrarPendienteRowDto>> ListarPendientesAsync(
        int oficinaId,
        int cajaDiarioId,
        int usuarioRegId,
        int personaId,
        CancellationToken cancellationToken = default);

    /// <summary>Valida que la fila CxC u orden CON pueda cobrarse en la oficina del token.</summary>
    Task<bool> PuedeCobrarAsync(
        int oficinaId,
        int ordenVentaId,
        int cuentaxCobrarId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad MVC <c>CreditoController.TieneCxcPendiente</c> por crédito.</summary>
    Task<bool> TienePendientesPorCreditoAsync(
        int creditoId,
        CancellationToken cancellationToken = default);
}
