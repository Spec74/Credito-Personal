using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public sealed class ReportesCatalogoCoberturaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportesCatalogoCoberturaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reportes_catalogo_cobertura_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/reportes/catalogo-cobertura");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reportes_catalogo_cobertura_con_jwt_devuelve_matriz_52()
    {
        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        tokenRes.EnsureSuccessStatusCode();
        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var token = tokenDoc.RootElement.GetProperty("accessToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reportes/catalogo-cobertura");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(52, root.GetProperty("totalCatalogo").GetInt32());
        Assert.Equal(46, root.GetProperty("completoDatosJsonCsvPdf").GetInt32());
        Assert.Equal(3, root.GetProperty("informesTextoRdlc").GetArrayLength());
        Assert.Equal(5, root.GetProperty("informesAdicionalesApi").GetArrayLength());
        Assert.Equal(0, root.GetProperty("soloMvc").GetInt32());
        Assert.True(root.GetProperty("parcial").GetInt32() >= 6);
    }
}
