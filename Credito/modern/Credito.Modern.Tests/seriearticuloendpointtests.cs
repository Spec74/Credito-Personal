using System.Net;

namespace Credito.Modern.Tests;

public class SerieArticuloEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SerieArticuloEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Series_articulo_sin_parametros_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/series-articulo", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Series_articulo_con_filtros_no_es_404()
    {
        var uri = new Uri("/api/v1/series-articulo?almacenId=1&articuloId=1", UriKind.Relative);
        var response = await _client.GetAsync(uri);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
