namespace Credito.Modern.Application.Auth;

public sealed record LegacyLoginOutcome(
    LegacyLoginStatus Status,
    int UsuarioId,
    int OficinaId,
    int UsuarioOficinaId,
    IReadOnlyList<string> Roles);
