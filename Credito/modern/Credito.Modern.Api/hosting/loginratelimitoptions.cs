namespace Credito.Modern.Api.Hosting;

/// <summary>Límites por IP para <c>POST /api/v1/auth/login</c> y <c>POST /api/v1/auth/refresh</c>.</summary>
public sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Si es <c>true</c>, no se aplica rate limiting (útil en tests de integración).</summary>
    public bool Disabled { get; set; }

    public int LoginPermitLimit { get; set; } = 15;

    public int LoginWindowSeconds { get; set; } = 60;

    /// <summary>Ventana para <c>POST /api/v1/auth/refresh</c> (independiente del login).</summary>
    public int RefreshPermitLimit { get; set; } = 60;

    public int RefreshWindowSeconds { get; set; } = 60;
}
