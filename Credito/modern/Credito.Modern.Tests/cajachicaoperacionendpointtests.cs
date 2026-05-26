using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CajaChicaOperacionEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CajaChicaOperacionEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Caja_chica_sesion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/caja-chica-sesion");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Movimientos_caja_chica_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/movimientos-caja-chica?tipo=E");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rendiciones_pendientes_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/rendiciones-pendientes-caja-chica");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crear_rendicion_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/rendiciones-caja-chica",
            new
            {
                movimientoCajaChicaId = 1,
                tipoDocumentoId = 1,
                fecha = "2026-01-01",
                serie = "F001",
                numero = "1",
                ruc = "20123456789",
                razonSocial = "TEST",
                detalleGasto = "GASTO",
                importe = 1,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cerrar_caja_chica_diario_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/credito/cerrar-caja-chica-diario", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Usuarios_buscar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/usuarios/buscar?term=ab");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
