namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaChicaDiarioReadService
{
    Task<CajaChicaSesionDto?> GetSesionAbiertaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovimientoCajaChicaRowDto>> ListarMovimientosAsync(
        int usuarioId,
        string tipo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RendicionPendienteRowDto>> ListarRendicionesPendientesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RendicionComprobanteRowDto>> ListarRendicionesAsync(
        int movimientoCajaChicaId,
        CancellationToken cancellationToken = default);

    Task<int> ContarRendicionesPendientesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<decimal?> GetSaldoFinalSesionAbiertaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
