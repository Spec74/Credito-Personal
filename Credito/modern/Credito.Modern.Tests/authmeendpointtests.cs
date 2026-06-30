using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class AuthMeEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthMeEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthMe_sin_bearer_devuelve_401()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthMe_con_dev_token_devuelve_200_y_ids()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 7, oficinaId = 3 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meRes = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        using var meDoc = await JsonDocument.ParseAsync(await meRes.Content.ReadAsStreamAsync());
        Assert.Equal(7, meDoc.RootElement.GetProperty("usuarioId").GetInt32());
        Assert.Equal(3, meDoc.RootElement.GetProperty("oficinaId").GetInt32());
        Assert.Equal(JsonValueKind.Array, meDoc.RootElement.GetProperty("roles").ValueKind);
    }

    [Fact]
    public async Task Registrar_acceso_ip_sin_bearer_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/auth/registrar-acceso-ip", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_acceso_ip_analista_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 7, oficinaId = 3, roles = new[] { "ANALISTA" } });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/registrar-acceso-ip");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
