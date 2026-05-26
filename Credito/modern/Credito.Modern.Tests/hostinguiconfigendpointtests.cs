using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public sealed class HostingUiConfigEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HostingUiConfigEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ui_config_es_publico_y_devuelve_campos()
    {
        var response = await _client.GetAsync("/api/v1/hosting/ui-config");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("useSpaForModule", out _));
        Assert.True(root.TryGetProperty("defaultLoginToSpa", out _));
        Assert.Equal("/app", root.GetProperty("spaBasePath").GetString());
    }

    [Fact]
    public async Task Ui_config_no_requiere_jwt()
    {
        var response = await _client.GetAsync("/api/v1/hosting/ui-config");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
