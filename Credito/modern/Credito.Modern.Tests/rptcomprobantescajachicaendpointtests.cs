using System.Net;

namespace Credito.Modern.Tests;

public sealed class RptComprobantesCajaChicaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptComprobantesCajaChicaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_comprobantes_caja_chica_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/rpt-comprobantes-caja-chica?fechaIni=2026-01-01&fechaFin=2026-01-31");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
