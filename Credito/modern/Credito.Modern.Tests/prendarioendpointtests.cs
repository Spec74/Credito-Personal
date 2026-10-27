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
    public async Task Devolver_prenda_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/devolver-prenda",
            new { oficinaId = 1, prendaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Devolucion_pdf_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/devolucion-pdf?oficinaId=1&prendaId=1");
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

    [Fact]
    public async Task Cobro_preview_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/cobro-preview?oficinaId=1&creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Actualizar_ficha_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/actualizar-ficha",
            new { oficinaId = 1, prendaId = 1L, observaciones = "RAYONES" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Renovar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/renovar",
            new { oficinaId = 1, cajaDiarioId = 1, creditoId = 1, dias = 30, tipoPagoId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Abonar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/abonar",
            new { oficinaId = 1, cajaDiarioId = 1, creditoId = 1, importeRecibido = 10m });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Liberar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/liberar",
            new { oficinaId = 1, cajaDiarioId = 1, creditoId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rematar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/prendario/rematar",
            new { oficinaId = 1, prendaId = 1L });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Foto_get_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/prendario/prenda-foto/1?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Foto_post_sin_jwt_devuelve_401()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "oficinaId");
        content.Add(new StringContent("1"), "prendaId");
        var response = await _client.PostAsync("/api/v1/prendario/prenda-foto", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
