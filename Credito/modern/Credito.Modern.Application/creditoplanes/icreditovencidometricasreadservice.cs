namespace Credito.Modern.Application.CreditoPlanes;

public interface ICreditoVencidoMetricasReadService
{
    /// <summary>Primera fila del proc, o <c>null</c> si no hay filas.</summary>
    Task<CreditoVencidoMetricasDto?> ObtenerAsync(int creditoId, CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>BovedaBL.CreditoVencido(null)</c> — totales en pantalla bóveda.</summary>
    Task<CreditoVencidoMetricasDto?> ObtenerCarteraAsync(CancellationToken cancellationToken = default);
}
