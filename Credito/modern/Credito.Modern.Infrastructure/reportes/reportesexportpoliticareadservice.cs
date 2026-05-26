using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Infrastructure.Reportes;

/// <summary>
/// Política estable de export (sin ejecutar RDLC). Base del motor PDF/Excel layout-fiel futuro.
/// </summary>
public sealed class ReportesExportPoliticaReadService : IReportesExportPoliticaReadService
{
    private static readonly IReadOnlyList<ReportePdfPilotoItemDto> InformesPdfTabular = BuildInformesPdfTabular();

    private static readonly ReporteExportPoliticaDto Politica = new(
        Fase: "6A-ui-sin-legacy-por-defecto; csv-tabular-completo; pdf-tabular-completo",
        CsvTabularCompleto: true,
        CatalogoModerno: "/api/v1/reportes/catalogo",
        PoliticaEndpoint: "/api/v1/reportes/politica-exportacion",
        InformesPdfPilotos: InformesPdfTabular,
        InformesTextoRdlcSinCsv:
        [
            new(
                "rpt-saldos-caja-resumen-ingreso",
                "/api/v1/credito/rpt-saldos-caja-resumen-ingreso",
                "CREDITO.usp_RptSaldosCajaResumenIngreso",
                "Respuesta JSON { texto } para pre-render RDLC; sin filas tabulares — no hay ruta -csv ni -pdf."),
            new(
                "rpt-saldos-caja-resumen-tipo-cuenta",
                "/api/v1/credito/rpt-saldos-caja-resumen-tipo-cuenta",
                "CREDITO.usp_RptSaldosCajaResumenTipoCuenta",
                "Respuesta JSON { texto }; sin filas tabulares — no hay ruta -csv ni -pdf."),
            new(
                "resumen-cuenta-boveda",
                "/api/v1/credito/resumen-cuenta-boveda",
                "CREDITO.usp_ResumenCuentaBoveda",
                "Respuesta JSON { texto }; sin filas tabulares — no hay ruta -csv ni -pdf."),
        ],
        RdlcMotor: new(
            Estado: "layout-rdlc-fase4-opcional",
            LegacyController: "VendixWeb.Controllers.ReporteController",
            CatalogoEndpoint: "/api/v1/reportes/catalogo",
            ProximaRebanada:
            "Render con layout idéntico a .rdlc (ReportViewer en host Windows o plantillas). Datos ya cubiertos por JSON + CSV + PDF tabular."));

    public Task<ReporteExportPoliticaDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(Politica);
    }

    private static IReadOnlyList<ReportePdfPilotoItemDto> BuildInformesPdfTabular()
    {
        return
        [
            Pdf("listar-saldo-cartera", "/api/v1/credito/listar-saldo-cartera"),
            Pdf("reporte-credito-movimiento", "/api/v1/credito/rpt-movimiento-credito"),
            Pdf("reporte-cajas-asignadas", "/api/v1/credito/rpt-cajas-asignadas"),
            Pdf("reporte-cobro-diario", "/api/v1/credito/rpt-cobro-diario"),
            Pdf("reporte-cobro-diario", "/api/v1/credito/rpt-cobro-diario-detalle", catalogIdOverride: "reporte-cobro-diario-detalle"),
            Pdf("reporte-cliente-bloqueado", "/api/v1/credito/rpt-clientes-bloqueados"),
            Pdf("reporte-cliente-tope-credito", "/api/v1/credito/rpt-clientes-tope-credito"),
            Pdf("reporte-aval", "/api/v1/credito/rpt-aval", catalogIdOverride: "reporte-aval"),
            Pdf("reporte-saldo-caja", "/api/v1/credito/rpt-saldos-caja"),
            Pdf("reporte-movimiento-boveda", "/api/v1/credito/rpt-movimiento-boveda"),
            Pdf("central-riesgo", "/api/v1/credito/central-riesgo-generar", catalogIdOverride: "central-riesgo-generar"),
            Pdf("reporte-credito", "/api/v1/credito/rpt-credito"),
            Pdf("reporte-credito-morosidad", "/api/v1/credito/rpt-credito-morosidad"),
            Pdf("reporte-credito-rentabilidad", "/api/v1/credito/rpt-credito-rentabilidad"),
            Pdf("reporte-credito-aprobado", "/api/v1/credito/rpt-credito-aprobacion"),
            Pdf("reporte-credito-activo", "/api/v1/credito/rpt-creditos-activos"),
            Pdf("reporte-credito-cierre", "/api/v1/credito/rpt-creditos-cierres"),
            Pdf("reporte-credito-moroso-pagado", "/api/v1/credito/rpt-creditos-morosos-pagados"),
            Pdf("reporte-clientes-inactivos", "/api/v1/credito/rpt-clientes-inactivos"),
            Pdf("reporte-caja-diario", "/api/v1/credito/rpt-caja-diario"),
            Pdf("reporte-credito-vencido", "/api/v1/credito/rpt-credito-vencido"),
            Pdf("reporte-comprobantes-caja-anulados", "/api/v1/credito/rpt-movimiento-caja-anulado"),
            Pdf("reporte-comprobantes-caja-chica", "/api/v1/credito/rpt-comprobantes-caja-chica"),
            Pdf("reporte-cliente", "/api/v1/credito/rpt-cliente"),
            Pdf("reporte-simulador-plan-pagos", "/api/v1/credito/rpt-simulador-plan-pagos"),
            Pdf("reporte-plan-pagos", "/api/v1/credito/rpt-plan-pagos"),
            Pdf("reporte-estado-credito", "/api/v1/credito/rpt-estado-credito"),
            Pdf("reporte-credito-tarea", "/api/v1/credito/rpt-credito-tarea"),
            Pdf("reporte-saldo-cartera-caja-diario", "/api/v1/credito/rpt-saldo-cartera-caja-diario"),
            Pdf("pagos-no-verificados", "/api/v1/credito/pagos-no-verificados"),
            Pdf("reporte-credito-observado", "/api/v1/credito/rpt-credito-observado"),
            Pdf("reporte-credito-condonado", "/api/v1/credito/rpt-credito-condonado"),
            Pdf("reporte-clientes-nuevos-mes", "/api/v1/credito/rpt-clientes-nuevos-mes"),
            Pdf("constancia-almacen", "/api/v1/almacen/rpt-constancia-almacen"),
            Pdf("reporte-kardex", "/api/v1/almacen/generar-kardex"),
            Pdf("reporte-stock", "/api/v1/almacen/reporte-stock"),
            Pdf("reporte-stock-anulados", "/api/v1/almacen/rpt-stock-anulados"),
            Pdf("reporte-lista-precio", "/api/v1/ventas/rpt-lista-precio"),
            Pdf("reporte-cod-barras", "/api/v1/ventas/codigo-barras-lst"),
            Pdf("reporte-rentabilidad-venta", "/api/v1/ventas/rpt-rentabilidad-venta", catalogIdOverride: "reporte-rentabilidad-venta"),
        ];
    }

    private static ReportePdfPilotoItemDto Pdf(
        string catalogId,
        string jsonBase,
        string? catalogIdOverride = null)
    {
        var id = catalogIdOverride ?? catalogId;
        return new(
            id,
            jsonBase,
            jsonBase + "-csv",
            jsonBase + "-pdf",
            "PDF tabular QuestPDF; mismas columnas que CSV (sin layout .rdlc).");
    }
}
