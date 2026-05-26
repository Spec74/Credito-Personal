using System.Net;

namespace Credito.Modern.Tests;

public class AlmacenEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AlmacenEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Almacenes_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/almacenes", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
