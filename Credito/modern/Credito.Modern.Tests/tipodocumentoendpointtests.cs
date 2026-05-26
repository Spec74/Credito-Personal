using System.Net;

namespace Credito.Modern.Tests;
public class TipoDocumentoEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TipoDocumentoEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Tipos_documento_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/tipos-documento", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tipos_documento_para_venta_ruta_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/tipos-documento?paraVenta=true", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
