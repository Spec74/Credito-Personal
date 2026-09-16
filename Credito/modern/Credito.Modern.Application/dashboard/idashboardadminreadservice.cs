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
    /// KPIs + cartera (respuesta rápida para progressive load).
    /// </summary>
    Task<DashboardAdminShellDto> ObtenerShellAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flujo, históricos y analistas (carga diferida).
    /// </summary>
    Task<DashboardAdminDetalleDto> ObtenerDetalleAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
