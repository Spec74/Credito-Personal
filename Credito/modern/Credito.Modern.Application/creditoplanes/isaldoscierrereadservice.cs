namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCierreReadService
{
    Task<ValidarCierreSaldosResponse> ValidarCierreMasivoAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad MVC: no filtra por oficina en <c>CajaChicaDiario</c>.</summary>
    Task<ValidarCierreSaldosResponse> ValidarCierreCajaChicaAsync(
        CancellationToken cancellationToken = default);
}
