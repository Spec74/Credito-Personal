using System.Net;

namespace Credito.Modern.Tests;

public sealed class RptConstanciaAlmacenEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptConstanciaAlmacenEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_constancia_almacen_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/almacen/rpt-constancia-almacen?movimientoId=1&oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
