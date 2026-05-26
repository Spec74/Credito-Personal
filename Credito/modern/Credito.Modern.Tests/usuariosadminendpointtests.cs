using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class UsuariosAdminEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsuariosAdminEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Usuarios_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/usuarios/gestion?page=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_usuario_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/usuarios/guardar",
            new
            {
                usuarioId = 0,
                apePaterno = "PEREZ",
                apeMaterno = "LOPEZ",
                nombre = "JUAN",
                numeroDocumento = "12345678",
                sexo = "M",
                nombreUsuario = "JPEREZ",
                claveUsuario = "test123",
                estado = true,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Asignar_oficinas_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/usuarios/1/asignar-oficinas",
            new { oficinaIds = new[] { 1 } });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Asignar_roles_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/usuarios/1/asignar-roles",
            new { oficinaId = 1, rolIds = new[] { 1 } });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
