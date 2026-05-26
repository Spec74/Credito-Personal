using System.Net;

namespace Credito.Modern.Tests;
public class OficinaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OficinaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Oficinas_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/oficinas", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
