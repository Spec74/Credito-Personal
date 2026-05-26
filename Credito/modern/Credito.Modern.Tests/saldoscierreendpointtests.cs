using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class SaldosCierreEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SaldosCierreEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Validar_cierre_saldos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/validar-cierre-saldos?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validar_cierre_caja_chica_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/validar-cierre-caja-chica?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Saldos_caja_diario_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/saldos-caja-diario?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Saldos_caja_chica_diario_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/saldos-caja-chica-diario");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Saldos_caja_diario_boveda_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/saldos-caja-diario-boveda?oficinaId=1&bovedaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Actualizar_datos_post_cierre_boveda_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/actualizar-datos-post-cierre-boveda",
            new { oficinaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validar_cierre_saldos_oficina_distinta_al_token_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/credito/validar-cierre-saldos?oficinaId=999");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Actualizar_datos_post_cierre_boveda_parametros_alineados_no_es_401()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/actualizar-datos-post-cierre-boveda")
        {
            Content = JsonContent.Create(new { oficinaId = 1 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
