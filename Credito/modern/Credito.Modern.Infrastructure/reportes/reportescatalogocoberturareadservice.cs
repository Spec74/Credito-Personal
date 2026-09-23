using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Infrastructure.Reportes;

/// <summary>
/// Matriz catálogo MVC (52) vs sustitutos modernos. Revisar al añadir informes en <see cref="ReportesCatalogoReadService"/>.
/// </summary>
public sealed class ReportesCatalogoCoberturaReadService : IReportesCatalogoCoberturaReadService
{
    private static readonly IReadOnlyList<ReporteCatalogoCoberturaItemDto> Items = BuildItems();

    private static readonly IReadOnlyList<ReporteCatalogoCoberturaItemDto> Adicionales = BuildAdicionales();

    private static readonly IReadOnlyList<ReporteCatalogoCoberturaItemDto> TextoRdlc = BuildTextoRdlc();

    private static readonly ReporteCatalogoCoberturaDto Snapshot = BuildSnapshot();

    public Task<ReporteCatalogoCoberturaDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(Snapshot);
    }

    private static ReporteCatalogoCoberturaDto BuildSnapshot()
    {
        var completoCatalogo = Items.Count(i => i.NivelCobertura == "completo-datos");
        var completoAdicionales = Adicionales.Count(i => i.NivelCobertura == "completo-datos");
        return new ReporteCatalogoCoberturaDto(
            Items.Count,
            completoCatalogo + completoAdicionales,
            Items.Count(i => i.NivelCobertura == "json"),
            Items.Count(i => i.NivelCobertura == "parcial") + Adicionales.Count(i => i.NivelCobertura == "parcial"),
            Items.Count(i => i.NivelCobertura == "solo-mvc"),
            Items.Count(i => i.NivelCobertura == "vista-indice"),
            Items,
            Adicionales,
            TextoRdlc);
    }

    private static IReadOnlyList<ReporteCatalogoCoberturaItemDto> BuildAdicionales() =>
    [
        C("listar-saldo-cartera", "Listar saldo cartera (proc)", "credito", "ListarSaldoCartera",
            "/api/v1/credito/listar-saldo-cartera", notaOverride: "No está en catálogo MVC; export operativo."),
        C("central-riesgo-generar", "Central de riesgos", "credito", "CentralRiesgoGenerar",
            "/api/v1/credito/central-riesgo-generar", notaOverride: "No está en catálogo MVC."),
        C("pagos-no-verificados", "Pagos no verificados", "credito", "PagosNoVerificados",
            "/api/v1/credito/pagos-no-verificados", notaOverride: "Grilla VerificarPagos; no fila catálogo."),
        C("reporte-rentabilidad-venta", "Rentabilidad ventas", "ventas", "ReporteRentabilidadVenta",
            "/api/v1/ventas/rpt-rentabilidad-venta",
            notaOverride: "En catálogo como reporte-avance-venta (distinto informe MVC)."),
        C("reporte-cobro-diario-detalle", "Cobro diario detalle (gestor)", "caja", "ReporteCobroDiario",
            "/api/v1/credito/rpt-cobro-diario-detalle",
            notaOverride: "Misma pantalla catálogo que cobro-diario; proc distinto."),
    ];

    private static IReadOnlyList<ReporteCatalogoCoberturaItemDto> BuildTextoRdlc() =>
    [
        new(
            "rpt-saldos-caja-resumen-ingreso",
            "Resumen ingresos caja",
            "caja",
            "ReporteSaldoCaja",
            "json-texto",
            "/api/v1/credito/rpt-saldos-caja-resumen-ingreso",
            null,
            null,
            "JSON { texto }; pre-render RDLC."),
        new(
            "rpt-saldos-caja-resumen-tipo-cuenta",
            "Resumen saldos por tipo cuenta",
            "caja",
            "ReporteSaldoCaja",
            "json-texto",
            "/api/v1/credito/rpt-saldos-caja-resumen-tipo-cuenta",
            null,
            null,
            "JSON { texto }; pre-render RDLC."),
        new(
            "resumen-cuenta-boveda",
            "Resumen cuenta bóveda",
            "caja",
            "ReporteMovimientoBoveda",
            "json-texto",
            "/api/v1/credito/resumen-cuenta-boveda",
            null,
            null,
            "JSON { texto }; pre-render RDLC."),
    ];

    private static IReadOnlyList<ReporteCatalogoCoberturaItemDto> BuildItems() =>
    [
        V("almacen", "Almacén — indicadores", "almacen", "Almacen", "vista-indice", nota: "Pantalla índice MVC; sin dataset de informe."),
        C("constancia-almacen", "Constancia almacén (movimiento)", "almacen", "ConstanciaAlmacen",
            "/api/v1/almacen/rpt-constancia-almacen",
            notaOverride: "Paridad ConstanciaAlmacen MVC (MovimientoBL + detalle); vista HTML legacy opcional."),
        C("reporte-kardex", "Reporte kardex", "almacen", "ReporteKardex",
            "/api/v1/almacen/generar-kardex"),
        C("reporte-cod-barras", "Reporte códigos de barras (movimiento)", "almacen", "ReporteCodBarras",
            "/api/v1/ventas/codigo-barras-lst",
            notaOverride: "VENTAS.usp_CodigoBarras_Lst; PDF etiquetas RDLC (rptCodigo) solo MVC."),
        C("reporte-stock", "Reporte stock", "almacen", "ReporteStock",
            "/api/v1/almacen/reporte-stock"),
        C("reporte-stock-anulados", "Reporte stock anulados", "almacen", "ReporteStockAnulados",
            "/api/v1/almacen/rpt-stock-anulados"),
        V("credito-vista", "Informes crédito — vista índice", "credito", "Credito", "vista-indice", nota: "Índice MVC."),
        V("venta-vista", "Informes venta — vista índice", "ventas", "Venta", "vista-indice", nota: "Índice MVC."),
        C("cobros-del-dia", "Cobros del día", "caja", "CobrosdelDia",
            "/api/v1/credito/rpt-cobro-diario",
            notaOverride: "Grilla MVC; mismos datos que reporte-cobro-diario (usp_RptCobroDiario)."),
        C("listar-cobros-del-dia-json", "Cobros del día (JSON)", "caja", "ListarCobrosDelDiaJson",
            "/api/v1/credito/rpt-cobro-diario",
            notaOverride: "JSON jqGrid MVC; mismo proc que cobros-del-dia / reporte-cobro-diario."),
        P("cobranza-pagos", "Cobranza pagos", "caja", "CobranzaPagos",
            "/api/v1/credito/rpt-cobranza-pagos", "/api/v1/credito/rpt-cobranza-pagos-csv",
            null,
            "SPA /reportes/cobranza; datos = usp_RptCobroDiarioDetalle. PDF tabular pendiente (Excel sí)."),
        P("obtener-cobranza-pagos", "Cobranza pagos (JSON)", "caja", "ObtenerCobranzaPagos",
            "/api/v1/credito/rpt-cobranza-pagos", "/api/v1/credito/rpt-cobranza-pagos-csv",
            null,
            "JSON; mismo proc que cobranza-pagos."),
        P("exportar-cobranza-pagos-excel", "Cobranza pagos (Excel)", "caja", "ExportarCobranzaPagosExcel",
            "/api/v1/credito/rpt-cobranza-pagos-excel", "/api/v1/credito/rpt-cobranza-pagos-csv",
            null,
            "Excel .xls HTML con estilos (paridad ExportarCobranzaPagosExcel MVC)."),
        C("reporte-cobro-diario", "Reporte cobro diario", "caja", "ReporteCobroDiario",
            "/api/v1/credito/rpt-cobro-diario"),
        C("generar-ruta-cobros-hoy", "Generar ruta cobros hoy", "operaciones", "GenerarRutaCobrosHoy",
            "POST /api/v1/credito/generar-ruta-cobros desde SPA cobro diario."),
        C("ruta-wa", "Ruta WhatsApp", "operaciones", "RutaWa",
            "GET /api/v1/credito/ruta-wa/{id} (anónimo, caché 4h)."),
        C("reporte-cajas-asignadas", "Reporte cajas asignadas", "caja", "ReporteCajasAsignadas",
            "/api/v1/credito/rpt-cajas-asignadas"),
        T("reporte-movimiento-caja", "Reporte movimiento caja", "caja", "ReporteMovimientoCaja",
            "/api/v1/credito/movimiento-caja-ticket-pdf",
            "Ticket PDF QuestPDF por movimientoCajaId (paridad ReporteMovimientoCaja; layout distinto RDLC)."),
        T("reporte-movimiento-caja-chica", "Reporte movimiento caja chica", "caja", "ReporteMovimientoCajaChica",
            "/api/v1/credito/movimiento-caja-chica-ticket-pdf",
            "Ticket PDF QuestPDF por movimientoCajaChicaId (sesión abierta del usuario JWT)."),
        C("reporte-comprobantes-caja-chica", "Reporte comprobantes caja chica", "caja", "ReporteComprobantesCajaChica",
            "/api/v1/credito/rpt-comprobantes-caja-chica"),
        C("reporte-comprobantes-caja-anulados", "Reporte comprobantes caja anulados", "caja",
            "ReporteComprobantesCajaAnulados", "/api/v1/credito/rpt-movimiento-caja-anulado",
            notaOverride: "Equivale a movimientos caja anulados (proc)."),
        P("reporte-saldo-caja-actual", "Reporte saldo caja actual", "caja", "ReporteSaldoCajaActual",
            "/api/v1/credito/rpt-saldos-caja", "/api/v1/credito/rpt-saldos-caja-csv",
            "/api/v1/credito/rpt-saldos-caja-pdf",
            "Usar rpt-saldos-caja con cajaDiarioId actual; layout RDLC distinto."),
        C("reporte-saldo-caja", "Reporte saldo caja diario", "caja", "ReporteSaldoCaja",
            "/api/v1/credito/rpt-saldos-caja"),
        P("reporte-saldo-caja-chica", "Reporte saldo caja chica diario", "caja", "ReporteSaldoCajaChica",
            "/api/v1/credito/rpt-saldos-caja", "/api/v1/credito/rpt-saldos-caja-csv",
            "/api/v1/credito/rpt-saldos-caja-pdf",
            "Mismo endpoint con indCajaChica; layout RDLC caja chica pendiente."),
        C("reporte-movimiento-boveda", "Reporte movimiento bóveda", "caja", "ReporteMovimientoBoveda",
            "/api/v1/credito/rpt-movimiento-boveda"),
        T("reporte-movimiento-boveda-mov", "Reporte movimiento bóveda (detalle)", "caja", "ReporteMovimientoBovedaMov",
            "/api/v1/credito/movimiento-boveda-ticket-pdf",
            "Ticket PDF QuestPDF por movimientoBovedaId (paridad ReporteMovimientoBovedaMov)."),
        C("reporte-saldo-cartera-caja-diario", "Reporte saldo cartera caja diario", "credito",
            "ReporteSaldoCarteraCajaDiario", "/api/v1/credito/rpt-saldo-cartera-caja-diario"),
        C("reporte-caja-diario", "Reporte caja diario", "caja", "ReporteCajaDiario",
            "/api/v1/credito/rpt-caja-diario"),
        C("reporte-simulador-plan-pagos", "Simulador plan de pagos", "credito", "ReporteSimuladorPlanPagos",
            "/api/v1/credito/rpt-simulador-plan-pagos",
            notaOverride: "usp_SimuladorCredito + cabecera ReporteSimuladorPlanPagos; ga CAP/CUO; layout RDLC solo MVC."),
        C("reporte-cliente", "Reporte cliente (persona)", "credito", "ReporteCliente",
            "/api/v1/credito/rpt-cliente",
            notaOverride: "Paridad ReporteCliente + usp_RptAval; layout RDLC ficha solo MVC."),
        C("reporte-plan-pagos", "Reporte plan de pagos (crédito)", "credito", "ReportePlanPagos",
            "/api/v1/credito/rpt-plan-pagos"),
        C("reporte-estado-credito", "Reporte estado crédito", "credito", "ReporteEstadoCredito",
            "/api/v1/credito/rpt-estado-credito"),
        C("reporte-credito", "Reporte créditos (filtros)", "credito", "ReporteCredito",
            "/api/v1/credito/rpt-credito"),
        C("reporte-credito-rentabilidad", "Reporte rentabilidad créditos", "credito", "ReporteCreditoRentabilidad",
            "/api/v1/credito/rpt-credito-rentabilidad"),
        C("reporte-credito-aprobado", "Reporte créditos aprobados", "credito", "ReporteCreditoAprobado",
            "/api/v1/credito/rpt-credito-aprobacion"),
        C("reporte-credito-morosidad", "Reporte morosidad créditos", "credito", "ReporteCreditoMorosidad",
            "/api/v1/credito/rpt-credito-morosidad"),
        C("reporte-clientes-nuevos-mes", "Reporte clientes nuevos del mes", "credito", "ReporteClientesNuevosMes",
            "/api/v1/credito/rpt-clientes-nuevos-mes"),
        C("reporte-cliente-tope-credito", "Reporte clientes tope crédito", "credito", "ReporteClienteTopeCredito",
            "/api/v1/credito/rpt-clientes-tope-credito"),
        C("reporte-cliente-bloqueado", "Reporte clientes bloqueados", "credito", "ReporteClienteBloqueado",
            "/api/v1/credito/rpt-clientes-bloqueados"),
        C("reporte-clientes-inactivos", "Reporte clientes inactivos", "credito", "ReporteClientesInactivos",
            "/api/v1/credito/rpt-clientes-inactivos"),
        C("reporte-clientes-inactivos-pagados", "Reporte clientes inactivos pagados", "credito",
            "ReporteClientesInactivosPagados", "/api/v1/credito/rpt-clientes-inactivos",
            notaOverride: "Mismo proc/API que inactivos con rango de fechas (paridad MVC pagados)."),
        C("reporte-credito-observado", "Reporte créditos observados", "credito", "ReporteCreditoObservado",
            "/api/v1/credito/rpt-credito-observado"),
        C("reporte-credito-condonado", "Reporte créditos condonados", "credito", "ReporteCreditoCondonado",
            "/api/v1/credito/rpt-credito-condonado"),
        C("reporte-credito-moroso-pagado", "Reporte créditos morosos pagados", "credito", "ReporteCreditoMorosoPagado",
            "/api/v1/credito/rpt-creditos-morosos-pagados"),
        C("reporte-credito-activo", "Reporte créditos activos", "credito", "ReporteCreditoActivo",
            "/api/v1/credito/rpt-creditos-activos"),
        C("reporte-credito-cierre", "Reporte créditos cierres", "credito", "ReporteCreditoCierre",
            "/api/v1/credito/rpt-creditos-cierres"),
        new(
            "reporte-morosidad-gestor",
            "Reporte morosidad por gestor",
            "credito",
            "ReporteMorosidadGestor",
            "completo-datos",
            "/api/v1/credito/rpt-cobro-diario",
            "/api/v1/credito/rpt-morosidad-gestor-csv",
            "/api/v1/credito/rpt-morosidad-gestor-pdf",
            "SPA /informes/morosidad-gestor (mora>0); CSV/PDF dedicados (mismo usp_RptCobroDiario)."),
        C("reporte-credito-movimiento", "Reporte movimientos crédito", "credito", "ReporteCreditoMovimiento",
            "/api/v1/credito/rpt-movimiento-credito"),
        C("reporte-credito-vencido", "Reporte créditos vencidos", "credito", "ReporteCreditoVencido",
            "/api/v1/credito/rpt-credito-vencido"),
        C("reporte-credito-tarea", "Reporte créditos tarea", "credito", "ReporteCreditoTarea",
            "/api/v1/credito/rpt-credito-tarea",
            notaOverride: "Listado + PDF tabular; filtro estado default PEN como MVC."),
        C("reporte-avance-venta", "Reporte avance venta", "ventas", "ReporteAvanceVenta",
            "/api/v1/ventas/rpt-rentabilidad-venta",
            notaOverride: "Mismo dataset que ReporteAvanceVenta MVC (usp_RptRentabilidadVenta)."),
        C("reporte-lista-precio", "Reporte lista de precios", "ventas", "ReporteListaPrecio",
            "/api/v1/ventas/rpt-lista-precio"),
    ];

    private static ReporteCatalogoCoberturaItemDto V(
        string id, string nombre, string area, string accion, string nivel, string? nota = null) =>
        new(id, nombre, area, accion, nivel, null, null, null, nota ?? string.Empty);

    private static ReporteCatalogoCoberturaItemDto M(
        string id, string nombre, string area, string accion, string nota) =>
        V(id, nombre, area, accion, "solo-mvc", nota);

    private static ReporteCatalogoCoberturaItemDto T(
        string id, string nombre, string area, string accion, string pdfPath, string nota) =>
        new(id, nombre, area, accion, "parcial", null, null, pdfPath, nota);

    private static ReporteCatalogoCoberturaItemDto J(
        string id, string nombre, string area, string accion,
        string json, string? csv, string? pdf, string nota) =>
        new(id, nombre, area, accion, "json", json, csv, pdf, nota);

    private static ReporteCatalogoCoberturaItemDto P(
        string id, string nombre, string area, string accion,
        string json, string? csv, string? pdf, string nota) =>
        new(id, nombre, area, accion, "parcial", json, csv, pdf, nota);

    private static ReporteCatalogoCoberturaItemDto C(
        string id, string nombre, string area, string accion, string jsonBase,
        string? notaOverride = null)
    {
        var nota = notaOverride ?? "JSON + CSV + PDF tabular (columnas = CSV).";
        return new(
            id,
            nombre,
            area,
            accion,
            "completo-datos",
            jsonBase,
            jsonBase + "-csv",
            jsonBase + "-pdf",
            nota);
    }
}
