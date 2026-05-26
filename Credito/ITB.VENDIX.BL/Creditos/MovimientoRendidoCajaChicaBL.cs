using ITB.VENDIX.DA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ITB.VENDIX.BL
{
    public class MovimientoRendidoCajaChicaBL : Repositorio<MovimientoRendidoCajaChica>
    {
        public static List<ReporteCajaChica> ReporteComprobantesCajaChica(DateTime pFechaInicio, DateTime pFechaFin)
        {
            var rpt = Listar(x => x.Fecha >= pFechaInicio && x.Fecha <= pFechaFin && x.MovimientoCajaChica.IndRendido == true, includeProperties: "TipoDocumento,MovimientoCajaChica")
                 .Select(x => new ReporteCajaChica
                 {
                     Gasto = x.MovimientoCajaChica.Descripcion,
                     Fecha = x.Fecha,
                     Documento = x.TipoDocumento.Denominacion,
                     Serie = x.Serie,
                     Numero = x.Numero,
                     RUC = x.RUC,
                     RazonSocial = x.RazonSocial,
                     DetalleGasto = x.DetalleGasto,
                     Importe = x.Importe
                 }).ToList();

            return rpt;
        }

        public class ReporteCajaChica
        {
            public string Gasto { get; set; }
            public DateTime Fecha { get; set; } 
            public string Documento { get; set; }
            public string Serie { get; set; }
            public string Numero { get; set; }
            public string RUC { get; set; }
            public string RazonSocial { get; set; }
            public string DetalleGasto { get; set; }
            public decimal Importe { get; set; }
        }
    }
}
