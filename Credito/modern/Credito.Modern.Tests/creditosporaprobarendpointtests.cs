using System.Net;

namespace Credito.Modern.Tests;

public sealed class CreditosPorAprobarEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreditosPorAprobarEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Creditos_por_aprobar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/creditos-por-aprobar?page=1&pageSize=15");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
