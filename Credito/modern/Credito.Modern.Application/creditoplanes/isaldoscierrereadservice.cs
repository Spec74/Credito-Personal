namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCierreReadService
{
    Task<ValidarCierreSaldosResponse> ValidarCierreMasivoAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<ValidarCierreSaldosResponse> ValidarCierreCajaChicaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Paridad <c>BovedaController.ValidarCierre</c> (previo a cerrar bóveda principal).
    /// Exige cajas cerradas/enviadas y cuadre de todas las bóvedas abiertas.
    /// </summary>
    Task<ValidarCierreSaldosResponse> ValidarCierreBovedaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Previo a cerrar solo la temporal: cajas cerradas/enviadas + cuadre de la temporal
    /// (no bloquea por descuadre de la principal).
    /// </summary>
    Task<ValidarCierreSaldosResponse> ValidarCierreBovedaTemporalAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
