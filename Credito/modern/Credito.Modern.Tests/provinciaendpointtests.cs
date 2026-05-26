using System.Net;

namespace Credito.Modern.Tests;

public class ProvinciaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProvinciaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Provincias_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/provincias", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
