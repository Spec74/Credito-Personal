using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class EntradaSalidaCajaDiarioEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EntradaSalidaCajaDiarioEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Entrada_salida_caja_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/entrada-salida-caja-diario",
            new
            {
                oficinaId = 1,
                cajaDiarioId = 1,
                personaId = 1,
                tipoOperacionId = 1,
                importe = 10m,
                descripcion = "test",
                tipoPagoId = 1,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Entrada_salida_caja_oficina_distinta_al_token_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 1, oficinaId = 1, roles = new[] { "ANALISTA" } });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/entrada-salida-caja-diario")
        {
            Content = JsonContent.Create(new
            {
                oficinaId = 999,
                cajaDiarioId = 1,
                personaId = 1,
                tipoOperacionId = 1,
                importe = 10m,
                descripcion = "test",
                tipoPagoId = 1,
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Entrada_salida_caja_sin_descripcion_devuelve_400()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 1, oficinaId = 1, roles = new[] { "ANALISTA" } });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/entrada-salida-caja-diario")
        {
            Content = JsonContent.Create(new
            {
                oficinaId = 1,
                cajaDiarioId = 1,
                personaId = 1,
                tipoOperacionId = 1,
                importe = 10m,
                descripcion = "  ",
                tipoPagoId = 1,
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Entrada_salida_caja_parametros_alineados_no_es_401()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 1, oficinaId = 1, roles = new[] { "ANALISTA" } });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/entrada-salida-caja-diario")
        {
            Content = JsonContent.Create(new
            {
                oficinaId = 1,
                cajaDiarioId = 1,
                personaId = 1,
                tipoOperacionId = 1,
                importe = 10m,
                descripcion = "smoke",
                tipoPagoId = 1,
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
