namespace Credito.Modern.Api.Hosting;

/// <summary>Confianza hacia proxies para <see cref="Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersMiddleware"/>.</summary>
public sealed class ForwardedHeadersBindingOptions
{
    public const string SectionName = "Hosting:ForwardedHeaders";

    /// <summary>Si es <c>true</c>, se usa el middleware y debe haber al menos una IP en <see cref="KnownProxies"/>.</summary>
    public bool Enabled { get; set; }

    /// <summary>IPs del proxy inmediato (balanceador, ARR, nginx) que pueden enviar <c>X-Forwarded-*</c>.</summary>
    public string[] KnownProxies { get; set; } = [];
}
