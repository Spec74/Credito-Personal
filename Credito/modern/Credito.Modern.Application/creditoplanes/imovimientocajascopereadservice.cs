namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaScopeReadService
{
    Task<MovimientoCajaScopeDto?> GetScopeAsync(int movimientoCajaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paridad MVC CajaDiario: INI con cuotas PAG → no se puede anular
    /// (el JSON legacy devolvía <c>true</c> y la UI bloqueaba).
    /// </summary>
    Task<bool> EsBloqueoAnularPorPagosCuotaAsync(int movimientoCajaId, CancellationToken cancellationToken = default);

    Task<MovimientoCajaAnularPreviewDto?> GetAnularPreviewAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default);
}
