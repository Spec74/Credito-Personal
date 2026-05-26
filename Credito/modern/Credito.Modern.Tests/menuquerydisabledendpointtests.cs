using System.Net;

namespace Credito.Modern.Tests;

public class MenuQueryDisabledEndpointTests : IClassFixture<CreditoMenuQueryDisabledWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MenuQueryDisabledEndpointTests(CreditoMenuQueryDisabledWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Menu_solo_con_query_cuando_menu_no_permite_query_devuelve_400()
    {
        var response = await _client.GetAsync(new Uri("/api/v1/menu?oficinaId=1&usuarioId=1", UriKind.Relative));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
