using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class SerieArticuloBL:Repositorio<SerieArticulo>
    {
        public static string ValidarExisteSerie(string pListaSerie, bool pIndCorrelativo,int? pCantidad)
        {
            using (var db = new VENDIXEntities())
            {
               return  db.usp_ExisteSerieArticulo(pListaSerie, pCantidad, pIndCorrelativo).ToList()[0];
            }
        }

        public static List<usp_CodigoBarras_Lst_Result> ListarArticuloCodigoBarras(int pMovimientoId)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_CodigoBarras_Lst(pMovimientoId).ToList();
            }
        }
        public static Int64 ObtenerUltimaSerie()
        {
            using (var db = new VENDIXEntities())
            {
                var reg = db.SerieArticulo.OrderByDescending(x => x.SerieArticuloId).Take(1).FirstOrDefault();
                return reg != null ? Int64.Parse(reg.NumeroSerie) : 10000;
            }
        }

        public static List<decimal> ObtenerIndicadoresAlmacen()
        {
            var ind = new List<decimal>();

            using (var db = new VENDIXEntities())
            {
                ind.Add(db.SerieArticulo.Where(x => x.EstadoId == 2).Select(x=>x.ArticuloId).Distinct().Count());
                ind.Add(db.SerieArticulo.Count(x => x.EstadoId == 2));
                ind.Add(db.SerieArticulo.Where(x => x.EstadoId == 2).Sum(x => x.MovimientoDet.PrecioUnitario));
                return ind;
            }
        }

        
    }
    
}
