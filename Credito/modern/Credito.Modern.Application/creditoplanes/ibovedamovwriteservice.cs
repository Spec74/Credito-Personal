namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaMovWriteService
{
    Task<BovedaMovOperacionResponse?> IngresoEgresoAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int tipoOperacionId,
        short tipoPagoId,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);

    Task<BovedaMovOperacionResponse?> TransferirACajaAsync(
        int oficinaId,
        int cajaId,
        decimal importe,
        string descripcion,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);

    /// <summary>Devuelve mensaje de error MVC o <c>null</c> si OK.</summary>
    Task<(string? Error, BovedaMovOperacionResponse? Result)> TransferirACajaChicaAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
