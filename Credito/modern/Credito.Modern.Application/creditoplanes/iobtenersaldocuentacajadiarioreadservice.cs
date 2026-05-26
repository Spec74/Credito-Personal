namespace Credito.Modern.Application.CreditoPlanes;

public interface IObtenerSaldoCuentaCajaDiarioReadService
{
    /// <summary>Primer escalar del proc, o <c>null</c> si no hay filas.</summary>
    Task<decimal?> ObtenerAsync(
        int cajaDiarioId,
        int tipoPagoId,
        CancellationToken cancellationToken = default);
}
