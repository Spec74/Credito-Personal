using System.Text.Json;

namespace Credito.Modern.Tests;

/// <summary>
/// Contrato mínimo de <c>appsettings.Production.json</c> antes del corte detrás de proxy.
/// </summary>
public sealed class StranglerProductionContractTests
{
    [Fact]
    public void Production_appsettings_cumple_contrato_corte()
    {
        var path = FindProductionSettingsPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var hosting = root.GetProperty("Hosting");

        Assert.False(hosting.GetProperty("AllowDevToken").GetBoolean());
        Assert.False(root.GetProperty("Menu").GetProperty("PermiteParametrosQuery").GetBoolean());
        Assert.True(root.GetProperty("Auth").GetProperty("RequerirClienteAcceso").GetBoolean());
        Assert.False(root.GetProperty("RateLimiting").GetProperty("Disabled").GetBoolean());

        var fwd = hosting.GetProperty("ForwardedHeaders");
        Assert.False(fwd.GetProperty("Enabled").GetBoolean());
        Assert.Equal(0, fwd.GetProperty("KnownProxies").GetArrayLength());
    }

    private static string FindProductionSettingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Credito.Modern.Api", "appsettings.Production.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("No se encontró Credito.Modern.Api/appsettings.Production.json");
    }
}
