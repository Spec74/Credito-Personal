namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaWriteService
{
    Task<CajaDiarioOperacionResponse> CerrarBovedaAsync(
        int oficinaId,
        int usuarioRegId,
        CancellationToken cancellationToken = default);

    Task<CajaDiarioOperacionResponse> CerrarBovedaTemporalAsync(
        int oficinaId,
        int usuarioRegId,
        CancellationToken cancellationToken = default);

    Task<CajaDiarioOperacionResponse> TransferirBovedaAsync(
        int bovedaInicioId,
        int bovedaDestinoId,
        string glosa,
        decimal monto,
        int usuarioRegId,
        int flagAceptar,
        int bovedaMovTempId,
        CancellationToken cancellationToken = default);
}
