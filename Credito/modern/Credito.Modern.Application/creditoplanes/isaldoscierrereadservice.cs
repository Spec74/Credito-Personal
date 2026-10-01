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
    /// Paridad <c>BovedaController.ValidarCierre</c> (previo a cerrar bóveda principal/temporal).
    /// </summary>
    Task<ValidarCierreSaldosResponse> ValidarCierreBovedaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
