using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class CalcularTemEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CalcularTemEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CalcularTem_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/calcular-tem",
            new { tea = 0.25m, formaPago = "M" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CalcularTem_con_jwt_dev_no_es_401_cuando_hay_token()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        using var calcReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/calcular-tem")
        {
            Content = JsonContent.Create(new { tea = 0.25m, formaPago = "M" }),
        };
        calcReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var calcRes = await _client.SendAsync(calcReq);
        Assert.NotEqual(HttpStatusCode.Unauthorized, calcRes.StatusCode);
    }

    [Fact]
    public async Task CalcularTem_formaPago_invalida_devuelve_400()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var calcReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credito/calcular-tem")
        {
            Content = JsonContent.Create(new { tea = 0.25m, formaPago = "X" }),
        };
        calcReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var calcRes = await _client.SendAsync(calcReq);
        Assert.Equal(HttpStatusCode.BadRequest, calcRes.StatusCode);
    }
}
