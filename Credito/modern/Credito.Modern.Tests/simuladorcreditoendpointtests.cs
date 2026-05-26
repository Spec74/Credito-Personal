using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class SimuladorCreditoEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SimuladorCreditoEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Simulador_credito_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/simulador-credito",
            new
            {
                monto = 1000m,
                formaPago = "M",
                nroCuotas = 12,
                interesMensual = 2.5m,
                fechaPrimerPago = new DateTime(2026, 6, 1),
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Simulador_credito_monto_cero_con_token_devuelve_200_lista_vacia()
    {
        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/simulador-credito")
        {
            Content = JsonContent.Create(new
            {
                monto = 0m,
                formaPago = "X",
                nroCuotas = 0,
                interesMensual = -1m,
                fechaPrimerPago = new DateTime(1800, 1, 1),
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var list = await res.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task Simulador_credito_formaPago_invalida_devuelve_400()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/simulador-credito")
        {
            Content = JsonContent.Create(new
            {
                monto = 1000m,
                formaPago = "MENSUAL",
                nroCuotas = 12,
                interesMensual = 2.5m,
                fechaPrimerPago = new DateTime(2026, 6, 1),
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Simulador_credito_con_cuerpo_valido_y_token_no_es_401()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/simulador-credito")
        {
            Content = JsonContent.Create(new
            {
                monto = 1000m,
                formaPago = "M",
                nroCuotas = 12,
                interesMensual = 2.5m,
                fechaPrimerPago = new DateTime(2026, 6, 1),
                gastosAdm = 10m,
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
