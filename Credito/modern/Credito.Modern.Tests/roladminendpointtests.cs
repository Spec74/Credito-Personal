using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class RolAdminEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RolAdminEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Roles_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/roles/gestion");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_rol_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/roles/guardar",
            new { rolId = 0, denominacion = "TEST", estado = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activar_rol_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/roles/1/activar", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Asignar_menus_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/roles/1/asignar-menus",
            new { menuIds = new[] { 1 } });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
