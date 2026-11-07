namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaDiarioTransferWriteService
{
    /// <summary>Paridad <c>CajaDiarioBL.TransferirSaldosCajaDiario</c> / <c>TransferirSaldosBoveda</c>.</summary>
    /// <param name="tipoPagoId">
    /// Medio que sale de caja y entra a bóveda (o a la otra caja). Default 1 = efectivo.
    /// Debe preservarse para Yape/Plin/BCP/Gerente/René (no forzar efectivo).
    /// </param>
    Task<(string? Error, bool Success)> TransferirSaldosAsync(
        int oficinaId,
        int cajaDiarioOrigenId,
        int usuarioRegId,
        decimal importe,
        string descripcion,
        int? cajaIdDestino,
        DateTime fechaOperacion,
        short tipoPagoId = 1,
        CancellationToken cancellationToken = default);
}
