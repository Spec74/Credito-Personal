using System.Net;

namespace Credito.Modern.Tests;

public class OcupacionEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OcupacionEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ocupaciones_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/ocupaciones", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
