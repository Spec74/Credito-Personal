using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class ArticuloBL : Repositorio<Articulo>
    {
        public static List<ItemCombo> BuscarArticuloSelect(string pClave)
        {
            var clave = pClave.Split(' ');
            var filterExpression = "Denominacion.Contains( \"" + clave[0] + "\")";
            for (var i = 1; i < clave.Count(); i++)
            {
                filterExpression += " && Denominacion.Contains( \"" + clave[i] + "\")";
            }
            
            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                return db.Articulo.Where(x => x.Estado).Where(filterExpression)
                   .Select(x => new ItemCombo { id = x.ArticuloId, value = x.Denominacion }).Take(10)
                   .ToList();
            }
        }
        public static List<ItemCombo> BuscarArticuloAllSelect(string pClave)
        {
            var clave = pClave.Split(' ');
            var filterExpression = "Denominacion.Contains( \"" + clave[0] + "\")";
            for (var i = 1; i < clave.Count(); i++)
            {
                filterExpression += " && Denominacion.Contains( \"" + clave[i] + "\")";
            }

            using (var db = new VENDIXEntities())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                return db.Articulo.Where(filterExpression)
                   .Select(x => new ItemCombo { id = x.ArticuloId, value = x.Denominacion }).Take(10)
                   .ToList();
            }
        }

        public static List<ListaArticulos> LstListaArticulosJGrid(GridDataRequest request, ref int pTotalItems)
        {
            var sClave = request.DataFilters()["Buscar"];
            var totalPuntos = 0;

            if (sClave != "0")
            {
                var cod = int.Parse(sClave);
                var tarjetaPunto = TarjetaPuntoBL.Obtener(x => x.PersonaId == cod);
                if (tarjetaPunto==null)
                    return new List<ListaArticulos>();

                totalPuntos = tarjetaPunto.TotalPuntos;
            }
            else
                return new List<ListaArticulos>();
            
            using (var db = new VENDIXEntities())
            {
                IQueryable<ListaArticulos> query = null;
                query = from lp in db.ListaPrecio
                        join a in db.Articulo on lp.ArticuloId equals a.ArticuloId
                        join ta in db.TipoArticulo on a.TipoArticuloId equals ta.TipoArticuloId
                        where a.Estado && lp.PuntosCanje <= totalPuntos && a.IndCanjeable == true
                        select new ListaArticulos()
                        {
                            ListaPrecioId = lp.ListaPrecioId,
                            TipoArticuloId = a.TipoArticuloId.Value,
                            TipoArticulo = ta.Denominacion,
                            ArticuloId = lp.ArticuloId,
                            ArticuloDesc = a.Denominacion,
                            PuntosCanje = lp.PuntosCanje,
                            Estado = lp.Estado
                        };

                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public class ListaArticulos : ListaPrecio
        {
            public int TipoArticuloId { get; set; }
            public string TipoArticulo { get; set; }
            public string ArticuloDesc { get; set; }
        }

    }

}
