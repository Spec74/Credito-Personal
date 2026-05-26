using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CanjearPuntosEndpointsTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CanjearPuntosEndpointsTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ventas_tarjeta_puntos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/ventas/tarjeta-puntos?personaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ventas_articulos_canjear_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/ventas/articulos-canjear?personaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ventas_canjear_puntos_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/canjear-puntos",
            new { personaId = 1, numeroSerie = "ABC123" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
