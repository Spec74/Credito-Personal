namespace Credito.Modern.Api.Hosting;

/// <summary>Confianza hacia proxies para <see cref="Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersMiddleware"/>.</summary>
public sealed class ForwardedHeadersBindingOptions
{
    public const string SectionName = "Hosting:ForwardedHeaders";

    /// <summary>Si es <c>true</c>, se usa el middleware. Con <see cref="KnownProxies"/> vacío se confía en el proxy de plataforma (App Service).</summary>
    public bool Enabled { get; set; }

    /// <summary>IPs del proxy inmediato (opcional). Vacío = confiar en X-Forwarded-* detrás de Azure/nginx.</summary>
    public string[] KnownProxies { get; set; } = [];
}
