namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaChicaDiarioWriteService
{
    /// <summary>Paridad <c>CajaChicaDiarioBL.TransferirCajaChicaDiarioBoveda</c>.</summary>
    Task<(string? Error, TransferirCierreCajaChicaResponse? Result)> TransferirCierreABovedaAsync(
        int oficinaId,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);

    Task<(string? Error, CerrarCajaChicaDiarioResponse? Result)> CerrarSesionAsync(
        int usuarioId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);

    Task<(string? Error, bool Result)> TransferirSaldosABovedaAsync(
        int oficinaId,
        int usuarioId,
        decimal importe,
        string descripcion,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
