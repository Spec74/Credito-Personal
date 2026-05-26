using System.Net;

namespace Credito.Modern.Tests;
public class MenuEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MenuEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Menu_sin_oficina_ni_usuario_devuelve_400()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/menu", UriKind.Relative));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Menu_con_parametros_cero_devuelve_400()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/menu?oficinaId=0&usuarioId=1", UriKind.Relative));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
