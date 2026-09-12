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
}
