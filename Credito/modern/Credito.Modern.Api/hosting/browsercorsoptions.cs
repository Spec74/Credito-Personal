namespace Credito.Modern.Api.Hosting;

/// <summary>Orígenes del navegador permitidos para CORS (SPA u otro front).</summary>
public sealed class BrowserCorsOptions
{
    public const string SectionName = "BrowserCors";

    /// <summary>Si está vacío: en Development se permite cualquier origen; en el resto de entornos no se autoriza ningún origen (hasta que configures valores reales).</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
