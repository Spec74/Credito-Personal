using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class RptListaPrecioGeneralEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptListaPrecioGeneralEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_lista_precio_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            new Uri("/api/v1/ventas/rpt-lista-precio", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rpt_lista_precio_csv_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            new Uri("/api/v1/ventas/rpt-lista-precio-csv", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rpt_lista_precio_marca_invalida_devuelve_400()
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
            "/api/v1/ventas/rpt-lista-precio?marcaId=0");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Rpt_lista_precio_csv_marca_invalida_devuelve_400()
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
            "/api/v1/ventas/rpt-lista-precio-csv?marcaId=0");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
    [Fact]
    public async Task Rpt_lista_precio_con_token_no_es_401()
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
            "/api/v1/ventas/rpt-lista-precio?indDescuento=true&indPuntos=false");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Rpt_lista_precio_csv_con_token_no_es_401()
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
            "/api/v1/ventas/rpt-lista-precio-csv?indDescuento=true&indPuntos=false");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
