using System.Net;

namespace Credito.Modern.Tests;

public class TipoArticuloEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TipoArticuloEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TiposArticulo_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/tipos-articulo", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
