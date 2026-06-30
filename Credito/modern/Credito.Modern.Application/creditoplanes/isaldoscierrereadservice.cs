namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCierreReadService
{
    Task<ValidarCierreSaldosResponse> ValidarCierreMasivoAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<ValidarCierreSaldosResponse> ValidarCierreCajaChicaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
