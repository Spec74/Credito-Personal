using System.Net;

namespace Credito.Modern.Tests;

public class DepartamentoEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DepartamentoEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Departamentos_ruta_registrada_no_es_404()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/departamentos", UriKind.Relative));
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
