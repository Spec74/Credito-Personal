using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class ArticulosCrudEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ArticulosCrudEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Articulos_gestion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/articulos/gestion?incluirInactivos=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Articulo_detalle_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/articulos/1/detalle");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Articulos_buscar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/articulos/buscar?term=test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_articulo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/articulos/guardar",
            new
            {
                articuloId = 0,
                modeloId = 1,
                tipoArticuloId = 1,
                codArticulo = "T1",
                denominacion = "Test",
                descripcion = "",
                monto = 10m,
                descuento = 0m,
                indPerecible = false,
                indImportado = false,
                indCanjeable = false,
                estado = true,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Articulo_imagenes_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/articulos/1/imagenes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Subir_imagen_articulo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/articulos/1/imagen", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_imagen_articulo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync(
            "/api/v1/articulos/1/eliminar-imagen?nombreArchivo=test.jpg",
            null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
