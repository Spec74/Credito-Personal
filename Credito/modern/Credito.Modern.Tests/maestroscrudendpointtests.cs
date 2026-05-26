using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class MaestrosCrudEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MaestrosCrudEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Marcas_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/marcas/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_marca_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/marcas/guardar",
            new { marcaId = 0, denominacion = "Test", estado = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activar_marca_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/marcas/1/activar", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Modelos_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/modelos/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_modelo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/modelos/guardar",
            new { modeloId = 0, marcaId = 1, denominacion = "Test", estado = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tipos_articulo_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/tipos-articulo/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_tipo_articulo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/tipos-articulo/guardar",
            new { tipoArticuloId = 0, denominacion = "Test", descripcion = "", estado = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Oficinas_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/oficinas/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_oficina_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/oficinas/guardar",
            new
            {
                oficinaId = 0,
                denominacion = "Test",
                descripcion = "",
                telefono = "",
                usuarioAsignadoId = 0,
                estado = true,
                indPrincipal = false,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Almacenes_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/almacenes/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_almacen_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacenes/guardar",
            new
            {
                almacenId = 0,
                oficinaId = 1,
                denominacion = "Test",
                descripcion = "",
                estado = true,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Lista_precios_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/lista-precios/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_lista_precio_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/lista-precios/guardar",
            new
            {
                listaPrecioId = 0,
                articuloId = 1,
                monto = 10m,
                descuento = 0m,
                puntos = 0,
                puntosCanje = 0,
                estado = true,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
