using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class AsignarCajaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AsignarCajaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cajas_para_asignar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/cajas-para-asignar?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Monto_boveda_asignacion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/monto-boveda-asignacion?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Asignar_caja_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/asignar-caja",
            new { oficinaId = 1, cajaId = 1, saldoInicial = 0m });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Asignar_caja_oficina_distinta_al_token_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/asignar-caja")
        {
            Content = JsonContent.Create(new { oficinaId = 999, cajaId = 1, saldoInicial = 0m }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Asignar_caja_saldo_negativo_devuelve_400()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/asignar-caja")
        {
            Content = JsonContent.Create(new { oficinaId = 1, cajaId = 1, saldoInicial = -1m }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Asignar_caja_parametros_alineados_no_es_401()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/asignar-caja")
        {
            Content = JsonContent.Create(new { oficinaId = 1, cajaId = 1, saldoInicial = 0m }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
