namespace Credito.Modern.Application.CreditoPlanes;

public interface IRptSaldosCajaResumenIngresoReadService
{
    /// <summary>Primera fila escalar texto del proc, o <c>null</c> si no hay filas.</summary>
    Task<string?> ObtenerPrimerTextoAsync(
        int cajaDiarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
