namespace Credito.Modern.Application.Auth;

/// <summary>
/// Identidad de sesión: ids del JWT + <c>NombreUsuario</c>/roles/oficina
/// leídos en vivo desde MAESTRO (no solo claims del access token).
/// </summary>
public sealed record AuthMeResponse(
    int UsuarioId,
    int OficinaId,
    int? UsuarioOficinaId,
    IReadOnlyList<string> Roles,
    string? NombreUsuario = null,
    string? OficinaDenominacion = null);
