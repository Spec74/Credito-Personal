using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Infrastructure.Reportes;

/// <summary>
/// Catálogo derivado de <c>VendixWeb.Controllers.ReporteController</c> (lista estable para revisión en PR).
/// </summary>
public sealed class ReportesCatalogoReadService : IReportesCatalogoReadService
{
    private static readonly IReadOnlyList<ReporteCatalogoItemDto> Catalog =
    [
        new("almacen", "Almacén — indicadores", "almacen", "Almacen"),
        new("constancia-almacen", "Constancia almacén (movimiento)", "almacen", "ConstanciaAlmacen"),
        new("reporte-kardex", "Reporte kardex", "almacen", "ReporteKardex"),
        new("reporte-cod-barras", "Reporte códigos de barras (movimiento)", "almacen", "ReporteCodBarras"),
        new("reporte-stock", "Reporte stock", "almacen", "ReporteStock"),
        new("reporte-stock-anulados", "Reporte stock anulados", "almacen", "ReporteStockAnulados"),
        new("credito-vista", "Informes crédito — vista índice", "credito", "Credito"),
        new("venta-vista", "Informes venta — vista índice", "ventas", "Venta"),
        new("cobros-del-dia", "Cobros del día", "caja", "CobrosdelDia"),
        new("cobranza-pagos", "Cobranza pagos", "caja", "CobranzaPagos"),
        new("obtener-cobranza-pagos", "Cobranza pagos (JSON)", "caja", "ObtenerCobranzaPagos"),
        new("exportar-cobranza-pagos-excel", "Cobranza pagos (Excel)", "caja", "ExportarCobranzaPagosExcel"),
        new("listar-cobros-del-dia-json", "Cobros del día (JSON)", "caja", "ListarCobrosDelDiaJson"),
        new("reporte-cobro-diario", "Reporte cobro diario", "caja", "ReporteCobroDiario"),
        new("generar-ruta-cobros-hoy", "Generar ruta cobros hoy", "operaciones", "GenerarRutaCobrosHoy"),
        new("ruta-wa", "Ruta WhatsApp", "operaciones", "RutaWa"),
        new("reporte-cajas-asignadas", "Reporte cajas asignadas", "caja", "ReporteCajasAsignadas"),
        new("reporte-movimiento-caja", "Reporte movimiento caja", "caja", "ReporteMovimientoCaja"),
        new("reporte-movimiento-caja-chica", "Reporte movimiento caja chica", "caja", "ReporteMovimientoCajaChica"),
        new("reporte-comprobantes-caja-chica", "Reporte comprobantes caja chica", "caja", "ReporteComprobantesCajaChica"),
        new("reporte-comprobantes-caja-anulados", "Reporte comprobantes caja anulados", "caja", "ReporteComprobantesCajaAnulados"),
        new("reporte-saldo-caja-actual", "Reporte saldo caja actual", "caja", "ReporteSaldoCajaActual"),
        new("reporte-saldo-caja", "Reporte saldo caja diario", "caja", "ReporteSaldoCaja"),
        new("reporte-saldo-caja-chica", "Reporte saldo caja chica diario", "caja", "ReporteSaldoCajaChica"),
        new("reporte-movimiento-boveda", "Reporte movimiento bóveda", "caja", "ReporteMovimientoBoveda"),
        new("reporte-movimiento-boveda-mov", "Reporte movimiento bóveda (detalle)", "caja", "ReporteMovimientoBovedaMov"),
        new("reporte-saldo-cartera-caja-diario", "Reporte saldo cartera caja diario", "credito", "ReporteSaldoCarteraCajaDiario"),
        new("reporte-caja-diario", "Reporte caja diario", "caja", "ReporteCajaDiario"),
        new("reporte-simulador-plan-pagos", "Simulador plan de pagos", "credito", "ReporteSimuladorPlanPagos"),
        new("reporte-cliente", "Reporte cliente (persona)", "credito", "ReporteCliente"),
        new("reporte-plan-pagos", "Reporte plan de pagos (crédito)", "credito", "ReportePlanPagos"),
        new("reporte-estado-credito", "Reporte estado crédito", "credito", "ReporteEstadoCredito"),
        new("reporte-credito", "Reporte créditos (filtros)", "credito", "ReporteCredito"),
        new("reporte-credito-rentabilidad", "Reporte rentabilidad créditos", "credito", "ReporteCreditoRentabilidad"),
        new("reporte-credito-aprobado", "Reporte créditos aprobados", "credito", "ReporteCreditoAprobado"),
        new("reporte-credito-morosidad", "Reporte morosidad créditos", "credito", "ReporteCreditoMorosidad"),
        new("reporte-clientes-nuevos-mes", "Reporte clientes nuevos del mes", "credito", "ReporteClientesNuevosMes"),
        new("reporte-cliente-tope-credito", "Reporte clientes tope crédito", "credito", "ReporteClienteTopeCredito"),
        new("reporte-cliente-bloqueado", "Reporte clientes bloqueados", "credito", "ReporteClienteBloqueado"),
        new("reporte-clientes-inactivos", "Reporte clientes inactivos", "credito", "ReporteClientesInactivos"),
        new("reporte-clientes-inactivos-pagados", "Reporte clientes inactivos pagados", "credito", "ReporteClientesInactivosPagados"),
        new("reporte-credito-observado", "Reporte créditos observados", "credito", "ReporteCreditoObservado"),
        new("reporte-credito-condonado", "Reporte créditos condonados", "credito", "ReporteCreditoCondonado"),
        new("reporte-credito-moroso-pagado", "Reporte créditos morosos pagados", "credito", "ReporteCreditoMorosoPagado"),
        new("reporte-credito-activo", "Reporte créditos activos", "credito", "ReporteCreditoActivo"),
        new("reporte-credito-cierre", "Reporte créditos cierres", "credito", "ReporteCreditoCierre"),
        new("reporte-morosidad-gestor", "Reporte morosidad por gestor", "credito", "ReporteMorosidadGestor"),
        new("reporte-credito-movimiento", "Reporte movimientos crédito", "credito", "ReporteCreditoMovimiento"),
        new("reporte-credito-vencido", "Reporte créditos vencidos", "credito", "ReporteCreditoVencido"),
        new("reporte-credito-tarea", "Reporte créditos tarea", "credito", "ReporteCreditoTarea"),
        new("reporte-avance-venta", "Reporte avance venta", "ventas", "ReporteAvanceVenta"),
        new("reporte-lista-precio", "Reporte lista de precios", "ventas", "ReporteListaPrecio"),
    ];

    public Task<IReadOnlyList<ReporteCatalogoItemDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(Catalog);
    }
}
