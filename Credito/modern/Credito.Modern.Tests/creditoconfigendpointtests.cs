using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CreditoConfigEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreditoConfigEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parametros_simulador_get_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/parametros-simulador");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Parametros_simulador_post_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/parametros-simulador",
            new { factorVariable = "1", factorFijo = "2" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Generar_ruta_cobros_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/generar-ruta-cobros",
            new { creditoIds = new[] { 1 } });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ruta_wa_sin_cache_devuelve_texto_expirado()
    {
        var response = await _client.GetAsync("/api/v1/credito/ruta-wa/noexiste");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("expirado", body, StringComparison.OrdinalIgnoreCase);
    }
}
