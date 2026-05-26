using System.Net;

namespace Credito.Modern.Tests;

public class MarcaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MarcaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Marcas_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/marcas", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
