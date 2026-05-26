namespace Credito.Modern.Api.Auth;

public sealed record DevTokenRequest(int UsuarioId, int OficinaId, IReadOnlyList<string>? Roles = null);

public sealed record DevTokenResponse(string AccessToken, int ExpiresInSeconds);
