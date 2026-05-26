namespace Credito.Modern.Application.CreditoPlanes;

public interface IObtenerMontoPendientePlanPagoReadService
{
    /// <summary>Primer valor escalar del proc, o <c>null</c> si no hay filas.</summary>
    Task<decimal?> ObtenerAsync(int oficinaId, CancellationToken cancellationToken = default);
}
