namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCajaDiarioReadService
{
    Task<IReadOnlyList<SaldoCajaSesionRowDto>> ListarCajaDiarioPorOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaldoCajaSesionRowDto>> ListarCajaChicaDiarioAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaldoCajaSesionRowDto>> ListarCajaDiarioBovedaAsync(
        int bovedaId,
        CancellationToken cancellationToken = default);
}
