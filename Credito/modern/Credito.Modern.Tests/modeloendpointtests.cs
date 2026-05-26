using System.Net;

namespace Credito.Modern.Tests;

public class ModeloEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ModeloEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Modelos_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/modelos", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Modelos_con_marcaId_query_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/modelos?marcaId=1", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
