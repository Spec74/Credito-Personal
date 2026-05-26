using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class AlmacenBL:Repositorio<Almacen>
    {
        public static List<Almacen> LstAlmacenJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "Denominacion.Contains( \"" + request.DataFilters()["Buscar"] + "\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<Almacen> query = db.Almacen.Include("Oficina");
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public static List<usp_GenerarKardex_Result> GenerarKardex(int pArticuloId, int pAlmacenId)
        {
            using (var db = new VENDIXEntities())
            {
              return  db.usp_GenerarKardex(pArticuloId, pAlmacenId).ToList();
            }
        }

        public static string ObtenerSerieKardex(int pMovimientoDetalleId,bool pIndStock)
        {
            using (var db = new VENDIXEntities())
            {
                return db.usp_ListarSerieKardex(pMovimientoDetalleId, pIndStock).ToList()[0];
            }
        }
    }
}
