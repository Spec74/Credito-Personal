using System.Net;

namespace Credito.Modern.Tests;

public class ReportesCatalogoEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportesCatalogoEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reportes_catalogo_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/reportes/catalogo");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
