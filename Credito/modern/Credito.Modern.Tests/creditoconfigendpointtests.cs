using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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
    public async Task Parametros_simulador_post_analista_devuelve_403()
    {
        var token = await DevTokenAsync(["ANALISTA"]);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/parametros-simulador")
        {
            Content = JsonContent.Create(new { factorVariable = "1", factorFijo = "2" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parametros_simulador_post_admin_valida_factores_antes_de_bd()
    {
        var token = await DevTokenAsync(["ADMINISTRADOR"]);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/parametros-simulador")
        {
            Content = JsonContent.Create(new { factorVariable = "abc", factorFijo = "2" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("numericos", body, StringComparison.OrdinalIgnoreCase);
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

    private async Task<string> DevTokenAsync(string[] roles)
    {
        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 1, oficinaId = 1, roles });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("accessToken").GetString() ?? string.Empty;
    }
}
