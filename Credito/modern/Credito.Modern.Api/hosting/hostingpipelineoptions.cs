namespace Credito.Modern.Api.Hosting;

/// <summary>Opciones de pipeline que no suelen activarse en integración local.</summary>
public sealed class HostingPipelineOptions
{
    public const string SectionName = "Hosting";

    /// <summary>Si es <c>true</c>, no se registra <c>UseHttpsRedirection</c> (útil en <c>WebApplicationFactory</c> sobre HTTP).</summary>
    public bool DisableHttpsRedirection { get; set; }

    /// <summary>Si es <c>false</c>, no se expone <c>POST /api/v1/dev/token</c> aunque el entorno sea Development.</summary>
    public bool AllowDevToken { get; set; }
}
