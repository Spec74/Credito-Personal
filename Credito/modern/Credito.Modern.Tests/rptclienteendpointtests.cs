using System.Net;

namespace Credito.Modern.Tests;

public sealed class RptClienteEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptClienteEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_cliente_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/rpt-cliente?personaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
