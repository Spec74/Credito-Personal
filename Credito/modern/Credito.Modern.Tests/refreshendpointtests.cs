using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class RefreshEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RefreshEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_sin_token_devuelve_400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_invalido_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = "no-es-un-jwt-valido" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
