using System.Net;

namespace Credito.Modern.Tests;

public class TipoOperacionEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TipoOperacionEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Tipo_operaciones_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/tipo-operaciones", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
