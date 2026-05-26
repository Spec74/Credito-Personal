using System.Net;
using System.Net.Http.Headers;

namespace Credito.Modern.Tests;

public class MenuJwtBearerTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MenuJwtBearerTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Menu_con_bearer_invalido_y_sin_query_devuelve_401()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/menu");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "no-es-un-jwt");
        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
