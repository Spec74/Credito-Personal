using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class TransferenciaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransferenciaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listar_transferencias_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/almacen/transferencias?oficinaId=1&page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Obtener_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/almacen/transferencia/1?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Detalle_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/almacen/transferencia/1/detalle?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crear_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/crear-transferencia",
            new { oficinaId = 1, almacenDestinoId = 2 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validar_serie_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/validar-serie-transferencia",
            new { oficinaId = 1, transferenciaId = 1, numeroSerie = "TEST" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_serie_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/eliminar-serie-transferencia",
            new { oficinaId = 1, transferenciaId = 1, articuloId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Desconfirmar_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/desconfirmar-transferencia",
            new { oficinaId = 1, transferenciaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirmar_transferencia_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/confirmar-transferencia",
            new { oficinaId = 1, transferenciaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
