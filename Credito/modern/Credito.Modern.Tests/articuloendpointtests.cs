using System.Net;

namespace Credito.Modern.Tests;

public class ArticuloEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ArticuloEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Articulos_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/articulos", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
