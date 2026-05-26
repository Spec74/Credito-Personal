using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

/// <summary>Contrato: todas las rutas <c>*-pdf</c> tabulares exigen JWT (401 sin Bearer).</summary>
public sealed class ReportPdfEndpointsUnauthorizedTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportPdfEndpointsUnauthorizedTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string> PdfRoutes => new(
        "/api/v1/credito/listar-saldo-cartera-pdf?anio=2026&mes=1&oficinaId=1&usuarioId=1",
        "/api/v1/credito/rpt-movimiento-credito-pdf?creditoId=1",
        "/api/v1/credito/rpt-cajas-asignadas-pdf?oficinaId=1&usuarioId=1",
        "/api/v1/credito/rpt-cobro-diario-pdf?oficinaId=1&usuarioId=1",
        "/api/v1/credito/rpt-cobro-diario-detalle-pdf?oficinaId=1&usuarioId=1",
        "/api/v1/credito/rpt-clientes-bloqueados-pdf?oficinaId=1&usuarioId=1",
        "/api/v1/credito/rpt-clientes-tope-credito-pdf?oficinaId=1&usuarioId=1",
        "/api/v1/credito/rpt-aval-pdf?personaId=1",
        "/api/v1/credito/rpt-saldos-caja-pdf?cajaDiarioId=1",
        "/api/v1/credito/rpt-movimiento-boveda-pdf?bovedaId=1",
        "/api/v1/credito/central-riesgo-generar-pdf?oficinaId=1&anio=2026&mes=1",
        "/api/v1/credito/central-riesgo-generar-txt?oficinaId=1&anio=2026&mes=1",
        "/api/v1/credito/central-riesgo-generar-txt?oficinaId=1&anio=2026&mes=1",
        "/api/v1/credito/rpt-credito-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31&estadoCredito=ACT",
        "/api/v1/credito/rpt-credito-morosidad-pdf?oficinaId=1&hastaFecha=2026-01-31&diasAtrazoIni=0&diasAtrazoFin=999",
        "/api/v1/credito/rpt-credito-rentabilidad-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31&estadoCredito=ACT",
        "/api/v1/credito/rpt-credito-aprobacion-pdf?oficinaId=1&fechaAprobacion=2026-01-01",
        "/api/v1/credito/rpt-creditos-activos-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-creditos-cierres-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-creditos-morosos-pagados-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-clientes-inactivos-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-caja-diario-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-credito-vencido-pdf?oficinaId=1",
        "/api/v1/credito/rpt-movimiento-caja-anulado-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/movimiento-caja-ticket-pdf?oficinaId=1&movimientoCajaId=1",
        "/api/v1/credito/movimiento-caja-chica-ticket-pdf?movimientoCajaChicaId=1",
        "/api/v1/credito/rpt-comprobantes-caja-chica-pdf?fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-plan-pagos-pdf?creditoId=1",
        "/api/v1/credito/rpt-estado-credito-pdf?creditoId=1",
        "/api/v1/credito/rpt-credito-tarea-pdf",
        "/api/v1/credito/movimiento-boveda-ticket-pdf?movimientoBovedaId=1",
        "/api/v1/credito/rpt-cliente-pdf?personaId=1",
        "/api/v1/ventas/codigo-barras-lst-pdf?movimientoId=1",
        "/api/v1/almacen/rpt-constancia-almacen-pdf?movimientoId=1&oficinaId=1",
        "/api/v1/credito/rpt-simulador-plan-pagos-pdf?productoId=1&monto=1000&nroCuotas=12&interesMensual=5&fechaPrimerPago=2026-06-01&formaPago=M",
        "/api/v1/credito/movimiento-caja-ticket-pdf?oficinaId=1&movimientoCajaId=1",
        "/api/v1/credito/movimiento-caja-chica-ticket-pdf?movimientoCajaChicaId=1",
        "/api/v1/credito/rpt-comprobantes-caja-chica-pdf?fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-plan-pagos-pdf?creditoId=1",
        "/api/v1/credito/rpt-estado-credito-pdf?creditoId=1",
        "/api/v1/credito/rpt-credito-tarea-pdf",
        "/api/v1/credito/movimiento-boveda-ticket-pdf?movimientoBovedaId=1",
        "/api/v1/credito/rpt-cliente-pdf?personaId=1",
        "/api/v1/ventas/codigo-barras-lst-pdf?movimientoId=1",
        "/api/v1/almacen/rpt-constancia-almacen-pdf?movimientoId=1&oficinaId=1",
        "/api/v1/credito/rpt-simulador-plan-pagos-pdf?productoId=1&monto=1000&nroCuotas=12&interesMensual=5&fechaPrimerPago=2026-06-01&formaPago=M",
        "/api/v1/credito/rpt-saldo-cartera-caja-diario-pdf?oficinaId=1&anioIni=2026&mesIni=1&anioFin=2026&mesFin=1",
        "/api/v1/credito/pagos-no-verificados-pdf?oficinaId=1",
        "/api/v1/credito/rpt-credito-observado-pdf?oficinaId=1",
        "/api/v1/credito/rpt-credito-condonado-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31",
        "/api/v1/credito/rpt-clientes-nuevos-mes-pdf?oficinaId=1",
        "/api/v1/almacen/generar-kardex-pdf?oficinaId=1&articuloId=1&almacenId=1",
        "/api/v1/almacen/generar-kardex-pdf?oficinaId=1&articuloId=1&almacenId=1",
        "/api/v1/almacen/reporte-stock-pdf?oficinaId=1",
        "/api/v1/almacen/reporte-stock-pdf?oficinaId=1",
        "/api/v1/almacen/rpt-stock-anulados-pdf",
        "/api/v1/ventas/rpt-lista-precio-pdf",
        "/api/v1/ventas/rpt-rentabilidad-venta-pdf?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31");

    [Theory]
    [MemberData(nameof(PdfRoutes))]
    public async Task Ruta_pdf_sin_jwt_devuelve_401(string path)
    {
        var response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Politica_exportacion_lista_32_pdf_tabulares()
    {
        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        tokenRes.EnsureSuccessStatusCode();
        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var token = tokenDoc.RootElement.GetProperty("accessToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reportes/politica-exportacion");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(
            "6A-ui-sin-legacy-por-defecto; csv-tabular-completo; pdf-tabular-completo",
            root.GetProperty("fase").GetString());
        Assert.Equal(40, root.GetProperty("informesPdfPilotos").GetArrayLength());
        Assert.Equal("layout-rdlc-fase4-opcional", root.GetProperty("rdlcMotor").GetProperty("estado").GetString());
    }
}
