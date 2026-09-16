namespace Credito.Modern.Application.Dashboard;

public interface IDashboardAdminReadService
{
    /// <summary>
    /// Indicadores de toda la oficina de la sesión. Nunca aceptar
    /// <paramref name="oficinaId"/> desde querystring.
    /// </summary>
    Task<DashboardAdminDto> ObtenerAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// KPIs only (respuesta rápida para progressive load; cartera llega en detalle).
    /// </summary>
    Task<DashboardAdminShellDto> ObtenerShellAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cartera, flujo, históricos y analistas (carga diferida).
    /// </summary>
    Task<DashboardAdminDetalleDto> ObtenerDetalleAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
