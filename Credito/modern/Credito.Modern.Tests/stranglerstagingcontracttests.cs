using System.Text.Json;

namespace Credito.Modern.Tests;

/// <summary>
/// Contrato mínimo de <c>appsettings.Staging.json</c> para proxy local (puerto 9080).
/// </summary>
public sealed class StranglerStagingContractTests
{
    [Fact]
    public void Staging_appsettings_cumple_contrato_proxy_local()
    {
        var path = FindStagingSettingsPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("Hosting").GetProperty("AllowDevToken").GetBoolean());
        Assert.True(root.GetProperty("Hosting").GetProperty("DisableHttpsRedirection").GetBoolean());
        Assert.False(root.GetProperty("Hosting").GetProperty("ForwardedHeaders").GetProperty("Enabled").GetBoolean());

        var origins = root.GetProperty("BrowserCors").GetProperty("AllowedOrigins");
        var allowed = origins.EnumerateArray().Select(e => e.GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("http://localhost:9080", allowed);
        Assert.Contains("http://127.0.0.1:9080", allowed);
        Assert.Contains("http://localhost:5173", allowed);
        Assert.Contains("http://127.0.0.1:5173", allowed);
        Assert.Contains("http://localhost:5173", allowed);
        Assert.Contains("http://127.0.0.1:5173", allowed);
    }

    private static string FindStagingSettingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Credito.Modern.Api", "appsettings.Staging.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("No se encontró Credito.Modern.Api/appsettings.Staging.json");
    }
}
