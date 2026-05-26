namespace Credito.Modern.Application.Auth;

/// <summary>Identidad resuelta desde el access JWT (claims <c>vendix:*</c> y roles MAESTRO).</summary>
public sealed record AuthMeResponse(
    int UsuarioId,
    int OficinaId,
    int? UsuarioOficinaId,
    IReadOnlyList<string> Roles);
