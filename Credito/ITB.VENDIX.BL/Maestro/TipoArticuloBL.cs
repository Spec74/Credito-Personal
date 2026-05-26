using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class TipoArticuloBL : Repositorio<TipoArticulo>
    {
        public static List<TipoArticulo> LstTipoArticuloJGrid(GridDataRequest request, ref int pTotalItems)
        {
            string filterExpression = string.Empty;

            if (request.DataFilters()["Buscar"] != string.Empty)
                filterExpression = "Denominacion.Contains( \"" + request.DataFilters()["Buscar"] + "\")";

            using (var db = new VENDIXEntities())
            {
                IQueryable<TipoArticulo> query = db.TipoArticulo;
                if (!String.IsNullOrEmpty(filterExpression))
                    query = query.Where(filterExpression);

                pTotalItems = query.Count();

                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();

            }
        }
    }
}
