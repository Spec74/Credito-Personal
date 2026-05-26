namespace Credito.Modern.Api.Hosting;

/// <summary>Política de cutover UI (Fase 5C-7). Expuesta en GET /api/v1/hosting/ui-config.</summary>
public sealed class HostingUiOptions
{
    public const string SectionName = "Ui";

    /// <summary>Si true, la SPA prioriza rutas modernas y oculta atajos al MVC salvo informes RDLC.</summary>
    public bool UseSpaForModule { get; set; }

    /// <summary>Si true, el proxy debe enviar / → /app/login (ver nginx spa-cutover).</summary>
    public bool DefaultLoginToSpa { get; set; }

    /// <summary>Ruta pública de la SPA (debe coincidir con VITE_BASE_URL).</summary>
    public string SpaBasePath { get; set; } = "/app";

    /// <summary>Ventana de observación post-redirect (documentación ops).</summary>
    public int ObservacionDias { get; set; } = 14;
}
