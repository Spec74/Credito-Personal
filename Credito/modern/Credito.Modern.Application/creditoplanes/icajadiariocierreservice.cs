namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaDiarioCierreService
{
    Task<ValidarCierreCajaDiarioResponse> ValidarCierreAsync(
        int cajaDiarioId,
        CancellationToken cancellationToken = default);

    Task<CerrarCajaDiarioResponse> CerrarAsync(
        int cajaDiarioId,
        int usuarioModId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
