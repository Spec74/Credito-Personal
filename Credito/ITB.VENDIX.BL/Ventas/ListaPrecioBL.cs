using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class ListaPrecioBL : Repositorio<ListaPrecio>
    {

        public static List<ListaPrecioBuscar> LstListaPrecioJGrid(GridDataRequest request, ref int pTotalItems)
        {
             
            var sClave = request.DataFilters()["Buscar"];
            var sTipoArticuloId = request.DataFilters()["TipoArticuloId"];
            var sAsignado = bool.Parse(request.DataFilters()["Asignado"]);

            string filterExpression = string.Empty;
            if (sClave != string.Empty && sTipoArticuloId == "0")
                filterExpression = "ArticuloDesc.Contains( \"" + sClave + "\")";
            else if (sClave == string.Empty && sTipoArticuloId != "0")
                filterExpression = "TipoArticuloId == " + sTipoArticuloId;
            else if (sClave != string.Empty && sTipoArticuloId != "0")
                filterExpression = "ArticuloDesc.Contains( \"" + sClave + "\") && TipoArticuloId == " + sTipoArticuloId;

            using (var db = new VENDIXEntities())
            {  
                IQueryable<ListaPrecioBuscar> query = null;
                if (sAsignado)
                {
                    query = from lp in db.ListaPrecio
                            join a in db.Articulo on lp.ArticuloId equals a.ArticuloId
                            join ta in db.TipoArticulo on a.TipoArticuloId equals ta.TipoArticuloId
                            where a.Estado
                            select new ListaPrecioBuscar
                                       {
                                           ListaPrecioId = lp.ListaPrecioId,
                                           TipoArticuloId = a.TipoArticuloId.Value,
                                           TipoArticulo = ta.Denominacion,
                                           ArticuloId = lp.ArticuloId,
                                           ArticuloDesc = a.Denominacion,
                                           Monto = lp.Monto,
                                           Descuento = lp.Descuento,
                                           PuntosCanje = lp.PuntosCanje,
                                           Puntos = lp.Puntos,
                                           Estado = lp.Estado
                                       };
                }
                else
                {
                    query = from a in db.Articulo
                            join ta in db.TipoArticulo on a.TipoArticuloId equals ta.TipoArticuloId
                            join lp in db.ListaPrecio on a.ArticuloId equals lp.ArticuloId
                            into articuloLista from fd in articuloLista.DefaultIfEmpty()
                            where a.Estado && fd == null
                            select new ListaPrecioBuscar
                                       {
                                           ListaPrecioId = 0,
                                           TipoArticuloId = a.TipoArticuloId.Value,
                                           TipoArticulo = ta.Denominacion,
                                           ArticuloId = a.ArticuloId,
                                           ArticuloDesc = a.Denominacion,
                                           Monto = 0,
                                           Descuento = 0,
                                           PuntosCanje = 0,
                                           Puntos = 0,
                                           Estado = true
                                       };
                }

                var buscarxcodigo = db.SerieArticulo.FirstOrDefault(x => x.NumeroSerie == sClave);
                int articuloid = buscarxcodigo == null ? 0 : buscarxcodigo.ArticuloId;

                if (articuloid==0)
                {
                    if (!String.IsNullOrEmpty(filterExpression))
                        query = query.Where(filterExpression);
                }
                else
                    query = query.Where("ArticuloId == " + articuloid);
                
                pTotalItems = query.Count();
                return query.OrderBy(request.sidx + " " + request.sord)
                    .Skip((request.page - 1) * request.rows).Take(request.rows).ToList();
            }
        }

        public class ListaPrecioBuscar:ListaPrecio
        {
            public int TipoArticuloId { get; set; }
            public string TipoArticulo { get; set; }
            public string ArticuloDesc { get; set; }
        }

        //public List<ListaPrecio> ListarListaPrecio(GridDataRequest request)
        //{
        //    string filterExpression = string.Empty;
        //    string sortExpression = request.sidx;
        //    string sortDirection = request.sord;
        //    int pageIndex = request.page - 1;
        //    int pageSize = request.rows;
            
        //    if (request.DataFilters()["Buscar"] != string.Empty)
        //        filterExpression = "Modelo.Denominacion.Contains( \"" + request.DataFilters()["Buscar"] + "\")";
            
        //    using (var db = new VENDIXEntities())
        //    {
        //        if (!String.IsNullOrEmpty(filterExpression))
        //            return db.ListaPrecio.Include("Modelo").Where(filterExpression).OrderBy(sortExpression + " " + sortDirection).Skip(
        //                    pageIndex * pageSize).Take(pageSize).ToList();
        //        return db.ListaPrecio.Include("Modelo").OrderBy(sortExpression + " " + sortDirection).Skip(pageIndex * pageSize).Take(pageSize).ToList();
        //    }
        //}

        //public List<ListaPrecio> ListarListaPrecio(string filterExpression, string sortExpression,
        //                                                string sortDirection, int pageIndex, int pageSize)
        //{
        //    using (var db = new VENDIXEntities())
        //    {
        //        if (!String.IsNullOrEmpty(filterExpression))
        //            return db.ListaPrecio.Include("Modelo").Where(filterExpression).OrderBy(sortExpression + " " + sortDirection).Skip(
        //                    pageIndex * pageSize).Take(pageSize).ToList();
        //        return db.ListaPrecio.Include("Modelo").OrderBy(sortExpression + " " + sortDirection).Skip(pageIndex * pageSize).Take(pageSize).ToList();

        //        // return ListarJGrid(sortExpression, sortDirection, pageIndex, pageSize, filterExpression,"Modelo");

        //    }
        //}
    }
}
