namespace Credito.Modern.Infrastructure.Integraciones;

public sealed class ApiPeruOptions
{
    public const string SectionName = "ApiPeru";

    public string BaseUrl { get; set; } = "https://apiperu.dev/api/";

    /// <summary>Bearer token (configurar en user-secrets / variables de entorno).</summary>
    public string Token { get; set; } = string.Empty;
}
