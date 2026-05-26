using System.Net;

namespace Credito.Modern.Tests;

public class TipoMovimientoAlmacenEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TipoMovimientoAlmacenEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Tipos_movimiento_almacen_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/tipos-movimiento-almacen", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
