namespace Credito.Modern.Application.Dashboard;

public interface IDashboardAnalistaReadService
{
    /// <summary>
    /// Indicadores del analista autenticado en su oficina. Nunca aceptar
    /// <paramref name="usuarioId"/> ni <paramref name="oficinaId"/> desde querystring.
    /// </summary>
    Task<DashboardAnalistaDto> ObtenerAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}
