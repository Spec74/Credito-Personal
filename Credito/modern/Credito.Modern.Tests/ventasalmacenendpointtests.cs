using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class VentasAlmacenEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VentasAlmacenEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Agregar_orden_venta_detalle_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/agregar-orden-venta-detalle",
            new { oficinaId = 1, ordenVentaId = 1, numeroSerie = "TEST" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Listar_ordenes_venta_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/ventas/ordenes-venta?oficinaId=1&entregado=false&page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Obtener_orden_venta_detalle_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/ventas/orden-venta/1?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crear_orden_venta_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/crear-orden-venta",
            new { oficinaId = 1, personaId = 1, tipoVenta = "CON" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Eliminar_orden_venta_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/eliminar-orden-venta",
            new { oficinaId = 1, ordenVentaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirmar_movimiento_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/confirmar-movimiento",
            new { oficinaId = 1, movimientoId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Movimientos_entrada_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/almacen/movimientos-entrada?oficinaId=1&almacenId=1&page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crear_movimiento_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/crear-movimiento",
            new { oficinaId = 1, almacenId = 1, tipoMovimientoId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Agregar_movimiento_documento_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/agregar-movimiento-documento",
            new { oficinaId = 1, movimientoId = 1, tipoDocumentoId = 1, serieDocumento = "F", nroDocumento = "1" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Buscar_serie_salida_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/almacen/buscar-serie-salida?numeroSerie=TEST");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Realizar_salida_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/realizar-salida",
            new
            {
                oficinaId = 1,
                tipoMovimientoId = 1,
                glosa = "test",
                series = new[] { new { serieId = 1, serie = "S1", articuloId = 1, denominacion = "X" } },
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Enviar_orden_venta_contado_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/enviar-orden-venta-contado",
            new { oficinaId = 1, ordenVentaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Caja_diario_venta_rapida_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/ventas/caja-diario-venta-rapida?oficinaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Articulo_venta_rapida_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/ventas/articulo-venta-rapida?oficinaId=1&codigo=TEST");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Realizar_pedido_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/realizar-pedido",
            new
            {
                oficinaId = 1,
                cajaDiarioId = 1,
                personaId = 1,
                pedidos = new[] { new { articuloId = 1, cantidad = 1, descuento = 0m } },
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Enviar_orden_venta_credito_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/ventas/enviar-orden-venta-credito",
            new { oficinaId = 1, ordenVentaId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Crear_movimiento_detalle_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/almacen/crear-movimiento-detalle",
            new
            {
                oficinaId = 1,
                movimientoId = 1,
                movimientoDetId = 0,
                articuloId = 1,
                indAutogenerar = false,
                listaSerie = "",
                cantidad = 1,
                indCorrelativo = false,
                precioUnitario = 1m,
                descuento = 0m,
                medida = 1,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
