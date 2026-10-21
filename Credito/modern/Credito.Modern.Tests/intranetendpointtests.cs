using System.Net;

namespace Credito.Modern.Tests;

public class IntranetEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IntranetEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/v1/comunicados")]
    [InlineData("/api/v1/bandeja")]
    [InlineData("/api/v1/agenda?desde=2026-10-01&hasta=2026-11-01")]
    public async Task Get_sin_jwt_devuelve_401(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
