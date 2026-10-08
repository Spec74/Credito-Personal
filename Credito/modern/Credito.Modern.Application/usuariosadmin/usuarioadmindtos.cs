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
    bool Estado);

public sealed record UsuarioGestionListItemDto(
    int UsuarioId,
    string NombreUsuario,
    string NombreCompleto,
    string? Celular1,
    string? EmailPersonal,
    bool Estado);

/// <summary>Usuario activo para filtros de reportes (gestor).</summary>
public sealed record UsuarioReporteGestorDto(
    int UsuarioId,
    string NombreUsuario,
    string NombreCompleto);

public sealed record UsuarioGestionPageDto(
    int Page,
    int PageSize,
    int TotalRecords,
    int TotalPages,
    List<UsuarioGestionListItemDto> Rows);

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
    List<OficinaAsignacionDto> Oficinas);

public sealed record OficinaAsignacionDto(int OficinaId, string Denominacion, bool Asignado);

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
