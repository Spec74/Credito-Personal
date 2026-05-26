namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaDiarioTransferWriteService
{
    /// <summary>Paridad <c>CajaDiarioBL.TransferirSaldosCajaDiario</c> / <c>TransferirSaldosBoveda</c>.</summary>
    Task<(string? Error, bool Success)> TransferirSaldosAsync(
        int oficinaId,
        int cajaDiarioOrigenId,
        int usuarioRegId,
        decimal importe,
        string descripcion,
        int? cajaIdDestino,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
