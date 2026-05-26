namespace Credito.Modern.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Credito.Modern";

    /// <summary>Audiencia del access token (Bearer en API).</summary>
    public string Audience { get; set; } = "Credito.Modern";

    /// <summary>Audiencia del refresh token (solo <c>POST /api/v1/auth/refresh</c>).</summary>
    public string RefreshAudience { get; set; } = "Credito.Modern.Refresh";

    /// <summary>Clave simétrica HS256; mínimo 32 bytes UTF-8.</summary>
    public string SigningKey { get; set; } = "";

    /// <summary>Duración del access token (login y dev).</summary>
    public int AccessTokenLifetimeHours { get; set; } = 8;

    /// <summary>Duración del refresh token emitido en login.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 7;

    /// <summary>
    /// Revisión de refresh tokens: incrementa (p. ej. vía <c>Jwt__RefreshTokenVersion</c>) para invalidar todos los refresh JWT emitidos con versión anterior.
    /// </summary>
    public int RefreshTokenVersion { get; set; } = 1;
}
