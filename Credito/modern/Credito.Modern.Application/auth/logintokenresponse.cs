namespace Credito.Modern.Application.Auth;

public sealed record LoginTokenResponse(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    int RefreshExpiresInSeconds,
    int UsuarioId,
    int OficinaId,
    int UsuarioOficinaId);
