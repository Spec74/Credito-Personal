using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.UsuariosAdmin;

public interface IUsuarioAdminReadService
{
    Task<UsuarioAdminScope?> ResolveScopeAsync(int oficinaId, CancellationToken cancellationToken = default);

    Task<UsuarioGestionPageDto> ListGestionAsync(
        UsuarioAdminScope scope,
        string? buscar,
        int page,
        int pageSize,
        bool incluirInactivos,
        bool verRed,
        int? sedeId,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>UsuarioBL.Listar(x =&gt; x.Estado)</c> en combos de reportes (CobranzaPagos, etc.).</summary>
    Task<IReadOnlyList<UsuarioReporteGestorDto>> ListReporteGestoresAsync(
        UsuarioAdminScope scope,
        CancellationToken cancellationToken = default);

    Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<PersonaPorDniDto?> GetPersonaPorDniAsync(string numeroDocumento, CancellationToken cancellationToken = default);

    Task<ValidarDniResponse> ValidarDniAsync(
        string numeroDocumento,
        int? usuarioId,
        CancellationToken cancellationToken = default);

    Task<List<RolAsignacionDto>> GetRolesAsignacionAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}

public interface IUsuarioAdminWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(
        UsuarioAdminScope scope,
        GuardarUsuarioRequest request,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> ActivarAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<ResetearClaveResponse> ResetearClaveAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> CambiarClaveAsync(
        int usuarioId,
        string claveActual,
        string claveNueva,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> AsignarOficinasAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        int[] oficinaIds,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> AsignarRolesAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        int oficinaId,
        int[] rolIds,
        CancellationToken cancellationToken = default);
}
