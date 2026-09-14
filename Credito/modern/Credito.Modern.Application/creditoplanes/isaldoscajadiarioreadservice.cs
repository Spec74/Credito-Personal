namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCajaDiarioReadService
{
    Task<SaldoCajaSesionPageDto> ListarCajaDiarioPorOficinaAsync(
        int oficinaId,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SaldoCajaSesionPageDto> ListarCajaChicaDiarioAsync(
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SaldoCajaSesionPageDto> ListarCajaDiarioBovedaAsync(
        int bovedaId,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
