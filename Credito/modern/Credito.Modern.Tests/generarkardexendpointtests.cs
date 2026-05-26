using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class GenerarKardexEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GenerarKardexEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Generar_kardex_sin_jwt_devuelve_401()
    {
        var uri = new Uri(
            "/api/v1/almacen/generar-kardex?oficinaId=1&articuloId=1&almacenId=1",
            UriKind.Relative);
        var response = await _client.GetAsync(uri);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Generar_kardex_csv_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(new Uri(
            "/api/v1/almacen/generar-kardex-csv?oficinaId=1&articuloId=1&almacenId=1",
            UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Generar_kardex_pdf_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(new Uri(
            "/api/v1/almacen/generar-kardex-pdf?oficinaId=1&articuloId=1&almacenId=1",
            UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Generar_kardex_oficina_distinta_al_token_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/almacen/generar-kardex?oficinaId=999&articuloId=1&almacenId=1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Generar_kardex_sin_articulo_id_con_token_devuelve_400()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/almacen/generar-kardex?oficinaId=1&almacenId=1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Generar_kardex_parametros_alineados_no_es_401()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/almacen/generar-kardex?oficinaId=1&articuloId=1&almacenId=1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
