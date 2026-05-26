using System.Net;

namespace Credito.Modern.Tests;

public class ListaPrecioEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ListaPrecioEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Lista_precios_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/lista-precios", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
