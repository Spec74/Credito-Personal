using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.UsuariosAdmin;

public interface IUsuarioAdminReadService
{
    Task<UsuarioGestionPageDto> ListGestionAsync(
        string? buscar,
        int page,
        int pageSize,
        bool incluirInactivos,
        CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>UsuarioBL.Listar(x =&gt; x.Estado)</c> en combos de reportes (CobranzaPagos, etc.).</summary>
    Task<IReadOnlyList<UsuarioReporteGestorDto>> ListReporteGestoresAsync(CancellationToken cancellationToken = default);

    Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<PersonaPorDniDto?> GetPersonaPorDniAsync(string numeroDocumento, CancellationToken cancellationToken = default);

    Task<ValidarDniResponse> ValidarDniAsync(
        string numeroDocumento,
        int? usuarioId,
        CancellationToken cancellationToken = default);

    Task<List<RolAsignacionDto>> GetRolesAsignacionAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);
}

public interface IUsuarioAdminWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(
        GuardarUsuarioRequest request,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> ActivarAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> ResetearClaveAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> AsignarOficinasAsync(
        int usuarioId,
        int[] oficinaIds,
        CancellationToken cancellationToken = default);

    Task<MaestroOperacionResponse> AsignarRolesAsync(
        int usuarioId,
        int oficinaId,
        int[] rolIds,
        CancellationToken cancellationToken = default);
}
