using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class BovedaMovEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BovedaMovEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cajas_abiertas_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/cajas-abiertas-transferencia-boveda?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingreso_egreso_boveda_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/ingreso-egreso-boveda",
            new { oficinaId = 1, importe = 1m, descripcion = "test", tipoOperacionId = 1, tipoPagoId = (short)1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Transferir_boveda_caja_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/transferir-boveda-caja",
            new { oficinaId = 1, cajaId = 1, importe = 1m, descripcion = "test" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingreso_egreso_boveda_sin_descripcion_devuelve_400()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/ingreso-egreso-boveda")
        {
            Content = JsonContent.Create(new
            {
                oficinaId = 1,
                importe = 1m,
                descripcion = "",
                tipoOperacionId = 1,
                tipoPagoId = (short)1,
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Transferir_boveda_caja_parametros_alineados_no_es_401()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/transferir-boveda-caja")
        {
            Content = JsonContent.Create(new { oficinaId = 1, cajaId = 1, importe = 1m, descripcion = "test" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
