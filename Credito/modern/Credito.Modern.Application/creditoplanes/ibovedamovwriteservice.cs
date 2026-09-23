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
        short tipoPagoOrigenId = 1,
        short tipoPagoDestinoId = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Paridad <c>TransferirAAnalista</c>: valida saldo del medio (ledger) y embudo a efectivo.
    /// Devuelve mensaje de negocio o resultado.
    /// </summary>
    Task<(string? Error, BovedaMovOperacionResponse? Result)> TransferirAAnalistaAsync(
        int oficinaId,
        int usuarioAnalistaId,
        short tipoPagoOrigenId,
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

    /// <summary>
    /// Paridad <c>BovedaMovBL.TransferirEntreBancos</c> → <c>usp_RegistrarTransferenciaBancos</c>.
    /// Tras el SP recalcula saldos con <c>usp_ActualizarSaldosBoveda</c> (el BL legado no lo hacía).
    /// </summary>
    Task<TransferirBovedaBancosResponse> TransferirEntreBancosAsync(
        int oficinaId,
        short tipoPagoOrigenId,
        short tipoPagoDestinoId,
        decimal importe,
        string glosa,
        int usuarioRegId,
        CancellationToken cancellationToken = default);
}
