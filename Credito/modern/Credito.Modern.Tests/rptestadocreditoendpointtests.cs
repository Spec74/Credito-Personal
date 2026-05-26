using System.Net;

namespace Credito.Modern.Tests;

public sealed class RptEstadoCreditoEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptEstadoCreditoEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_estado_credito_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/rpt-estado-credito?creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
