using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class PlaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parametro_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/pla/parametro");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_parametro_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/pla/parametro",
            new { tcVentaMesAnterior = 3.75m, tcPeriodo = "2026-09" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Candidatos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/pla/candidatos-ro?oficinaId=1&anio=2026&mes=10");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inusual_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/pla/inusual?oficinaId=1&anio=2026&mes=10");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_inusual_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/pla/inusual",
            new { oficinaId = 1, personaId = 1, calificacion = "INUSUAL", motivo = "Prueba" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
