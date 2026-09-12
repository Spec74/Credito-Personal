using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CreditoCondonacionEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreditoCondonacionEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/condonaciones-pendientes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pendiente_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/condonacion-pendiente?creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Solicitar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/solicitar-condonacion",
            new { oficinaId = 1, cajaDiarioId = 1, creditoId = 1, moraCondonacion = 10m });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_sin_jwt_devuelve_401()
    {
        var response = await _client.DeleteAsync("/api/v1/credito/condonaciones-pendientes/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
