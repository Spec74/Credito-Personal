using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class PrendarioEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PrendarioEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Resumen_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/resumen?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Listado_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/creditos?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crear_solicitud_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/crear-solicitud",
            new { oficinaId = 1, personaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Prendas_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/prendas?oficinaId=1&creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_prendas_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/guardar-prendas",
            new { oficinaId = 1, creditoId = 1, prendas = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Contrato_pdf_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/contrato-pdf?oficinaId=1&creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Acta_pdf_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/acta-entrega-pdf?oficinaId=1&creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Avisos_vencimiento_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/avisos-vencimiento?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enviar_avisos_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/avisos-vencimiento/enviar",
            new { oficinaId = 1, diasAntes = 3 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Estado_avisos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/avisos-vencimiento/estado");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
