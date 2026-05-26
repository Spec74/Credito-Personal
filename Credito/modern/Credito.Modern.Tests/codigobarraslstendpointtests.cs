using System.Net;

namespace Credito.Modern.Tests;

public class CodigoBarrasLstEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CodigoBarrasLstEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Codigo_barras_lst_sin_movimiento_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/ventas/codigo-barras-lst", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Codigo_barras_lst_con_movimiento_no_es_404()
    {
        var uri = new Uri("/api/v1/ventas/codigo-barras-lst?movimientoId=1", UriKind.Relative);
        var response = await _client.GetAsync(uri);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
