using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CajaMaestroEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CajaMaestroEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cajas_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/cajas/gestion?page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cajas_gestores_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/cajas/gestores-activos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_caja_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/cajas/guardar",
            new
            {
                cajaId = 0,
                oficinaId = 1,
                denominacion = "Test",
                cajeroId = (int?)null,
                estado = true,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activar_caja_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/cajas/1/activar", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
