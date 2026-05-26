namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaAsignacionReadService
{
    Task<List<CajaParaAsignarRowDto>> ListarCajasParaAsignarAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<decimal> ObtenerMontoBovedaAsignacionAsync(
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
