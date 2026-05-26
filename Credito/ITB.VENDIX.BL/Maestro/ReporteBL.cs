using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class ReporteBL
    {

        public static List<ReporteListaPrecioGeneral> ListarReporteListaPrecio(int? pMarcaId, bool indDescuento, bool pIndPuntos)
        {
            var filtro = "Estado";
            using (var db = new VENDIXEntities())
            {

                if (pMarcaId.HasValue)
                    filtro += " && Articulo.Modelo.MarcaId = " + pMarcaId.Value.ToString();
                if (indDescuento)
                    filtro += " && Descuento > 0";
                if (pIndPuntos)
                    filtro += " && PuntosCanje > 0 ";

                var qry = db.ListaPrecio.Where(filtro).Select(
                    x => new ReporteListaPrecioGeneral
                    {
                        ArticuloId = x.ArticuloId,
                        TipoArticulo = x.Articulo.TipoArticulo.Denominacion,
                        ArticuloDes = x.Articulo.Denominacion,
                        Monto = x.Monto.Value,
                        Descuento = x.Descuento,
                        PuntosCanje = x.PuntosCanje
                    }).OrderBy(x => x.TipoArticulo);
                return qry.ToList();
            }
        }
        public static List<usp_ReporteStock_Result> ListarReporteStockGeneral(int? pOficinaId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_ReporteStock(pOficinaId).ToList();
            }
        }
        public static List<ReporteStockAnulado> ListarReporteStockAnulados()
        {
            using (var db = new VENDIXEntities())
            {
                var qry = from f in db.MovimientoDet
                          where f.Movimiento.EstadoId == 3 && f.Movimiento.TipoMovimientoId != 2 && f.Movimiento.TipoMovimiento.IndEntrada == false
                          && f.Movimiento.TipoMovimiento.IndDevolucion == false && f.Movimiento.TipoMovimiento.IndTransferencia == false
                          select new ReporteStockAnulado
                          {
                              MovimientoId = f.MovimientoId,
                              Movimiento = f.Movimiento.TipoMovimiento.Descripcion,
                              Observacion = f.Movimiento.Observacion,
                              Fecha = f.Movimiento.Fecha,
                              Cantidad = f.Cantidad,
                              Detalle = f.Descripcion
                          };
                return qry.OrderBy(x => x.Fecha).ToList();
            }
        }

        public static List<usp_RptCredito_Result> ListarReporteCredito(int? pOficinaId, int? pGestorid, string pEstadoCredito, DateTime pFechaIni, DateTime pFechaFin)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptCredito(pOficinaId, pGestorid, pEstadoCredito, pFechaIni, pFechaFin).ToList();
            }
        }
        public static List<usp_RptRentabilidadVenta_Result> ListarReporteRentabilidadVenta(DateTime pFechaIni, DateTime pFechaFin, bool indContado, bool indCredito, int? pOficinaId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_RptRentabilidadVenta(pFechaIni, pFechaFin, indContado, indCredito, pOficinaId).ToList();
            }
        }
        public static List<usp_CentralRiesgoGenerar_Result> ListarReporteCentralRiesgo(int? pOficinaId, int pAnio, int pMes )
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_CentralRiesgoGenerar(pOficinaId, pAnio, pMes).ToList();
            }
        }

    }

    public class ReporteStockAnulado{
        public int MovimientoId { get; set; }
        public string Movimiento { get; set; }
        public string Observacion { get; set; }
        public DateTime Fecha { get; set; }
        public int Cantidad { get; set; }
        public string Detalle { get; set; }
    }
    public class ReporteListaPrecioGeneral:ListaPrecio
    {
        public string TipoArticulo { get; set; }
        public string ArticuloDes { get; set; }
    }
    
}
