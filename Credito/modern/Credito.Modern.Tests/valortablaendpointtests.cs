using System.Net;

namespace Credito.Modern.Tests;

public class ValorTablaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ValorTablaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Valores_tabla_sin_tablaId_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/valores-tabla", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Valores_tabla_con_tablaId_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/valores-tabla?tablaId=10", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
