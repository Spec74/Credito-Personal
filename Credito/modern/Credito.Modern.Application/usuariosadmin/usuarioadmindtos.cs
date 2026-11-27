using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.UsuariosAdmin;

public sealed record GuardarUsuarioRequest(
    int UsuarioId,
    string ApePaterno,
    string ApeMaterno,
    string Nombre,
    string NumeroDocumento,
    string Sexo,
    DateTime? FechaNacimiento,
    string? TelefonoMovil,
    string? EmailPersonal,
    string? Direccion,
    string NombreUsuario,
    string ClaveUsuario,
    bool Estado,
    int[]? OficinaIds = null,
    int[]? RolIds = null);

public sealed record UsuarioGestionListItemDto(
    int UsuarioId,
    string NombreUsuario,
    string NombreCompleto,
    string? Celular1,
    string? EmailPersonal,
    bool Estado,
    bool EnEstaSede = true,
    string? Sedes = null);

/// <summary>Usuario activo para filtros de reportes (gestor).</summary>
public sealed record UsuarioReporteGestorDto(
    int UsuarioId,
    string NombreUsuario,
    string NombreCompleto);

/// <summary>
/// Página de gestión. <see cref="EsCentral"/> = IndPrincipal de la sede de sesión;
/// <see cref="AlcanceRed"/> = permiso de red (no confundir con EsCentral).
/// </summary>
public sealed record UsuarioGestionPageDto(
    int Page,
    int PageSize,
    int TotalRecords,
    int TotalPages,
    List<UsuarioGestionListItemDto> Rows,
    bool EsCentral = false,
    int OficinaId = 0,
    string? Oficina = null,
    bool VerRed = false,
    bool AlcanceRed = false);

public sealed record UsuarioPersonaDetalleDto(
    int UsuarioId,
    string NombreUsuario,
    bool Estado,
    int PersonaId,
    string ApePaterno,
    string ApeMaterno,
    string Nombre,
    string NombreCompleto,
    string NumeroDocumento,
    string? Sexo,
    DateTime? FechaNacimiento,
    string? Celular1,
    string? EmailPersonal,
    string? Direccion,
    List<OficinaAsignacionDto> Oficinas,
    bool PuedeAsignarOtrasOficinas = false,
    bool PuedeDesactivarCuenta = false,
    bool EnEstaSede = false,
    IReadOnlyList<string>? OtrasSedes = null);

public sealed record OficinaAsignacionDto(
    int OficinaId,
    string Denominacion,
    bool Asignado,
    bool Editable = true);

/// <summary>
/// Sede de la sesión JWT para acotar gestión de usuarios.
/// <para>
/// <see cref="AlcanceRed"/> = admin de red (rol ADMINISTRADOR + oficina Central).
/// El gerente de sede (ENCARGADO) siempre opera solo <see cref="OficinaId"/>,
/// aunque su sesión sea Central.
/// </para>
/// <para>
/// <see cref="ActorEsAdministrador"/> lo fija el endpoint desde claims JWT;
/// al resolver la oficina desde BD queda en false hasta enriquecer el scope.
/// </para>
/// </summary>
public sealed record UsuarioAdminScope(
    int OficinaId,
    bool EsCentral,
    string Oficina,
    bool ActorEsAdministrador)
{
    /// <summary>Puede listar/mutar usuarios de toda la red.</summary>
    public bool AlcanceRed => EsCentral && ActorEsAdministrador;
}

public sealed record RolAsignacionDto(int RolId, string Denominacion, bool Asignado);

public sealed record PersonaPorDniDto(
    int PersonaId,
    string ApePaterno,
    string ApeMaterno,
    string Nombre,
    string NombreCompleto,
    string NumeroDocumento,
    string? Sexo,
    DateTime? FechaNacimiento,
    string? Celular1,
    string? EmailPersonal,
    string? Direccion);

public sealed record ValidarDniResponse(bool Existe);

public sealed record AsignarOficinasRequest(int[] OficinaIds);

public sealed record AsignarRolesRequest(int OficinaId, int[] RolIds);

/// <summary>Respuesta de reset: incluye clave temporal de un solo uso (no se vuelve a consultar).</summary>
public sealed record ResetearClaveResponse(bool Success, int? Id, string? Mensaje, string? ClaveTemporal);

public sealed record CambiarClaveRequest(string ClaveActual, string ClaveNueva);
