using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class TareasEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TareasEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listar_tareas_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/tareas?estado=PEN");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Puede_editar_tarea_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/tareas/puede-editar");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Buscar_creditos_tarea_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/tareas/creditos-buscar?term=test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Obtener_tarea_detalle_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/tareas/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_tarea_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/tareas/guardar",
            new { tareaId = 0, creditoId = 1, subtareas = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_tarea_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/credito/tareas/1/eliminar", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Completar_tarea_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/tareas/1/completar",
            new { completada = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
