using System.Net;

namespace Credito.Modern.Tests;

public class ProductoEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductoEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Productos_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/productos", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
