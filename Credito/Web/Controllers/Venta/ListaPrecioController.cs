using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;


namespace VendixWeb.Controllers
{
    public class ListaPrecioController : Controller
    {

        public ActionResult Index()
        {
            var lista = TipoArticuloBL.Listar(x => x.Estado);
            lista.Insert(0, new TipoArticulo { TipoArticuloId = 0, Denominacion = "TODOS LOS TIPOS" });
            ViewBag.cboTipoArticulo = new SelectList(lista, "TipoArticuloId", "Denominacion");
            return View();
            //return PartialView();
        }

        public ActionResult Consulta()
        {
            var lista = TipoArticuloBL.Listar(x => x.Estado);
            lista.Insert(0, new TipoArticulo { TipoArticuloId = 0, Denominacion = "TODOS LOS TIPOS" });
            ViewBag.cboTipoArticulo = new SelectList(lista, "TipoArticuloId", "Denominacion");
            return View();
        }

        public ActionResult Listar(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = ListaPrecioBL.LstListaPrecioJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.ArticuloId,
                            cell = new string[] { 
                                                    item.ArticuloId.ToString(),
                                                    item.ListaPrecioId.ToString(),
                                                    item.TipoArticulo,
                                                    item.ArticuloDesc,
                                                    item.Monto.ToString(),
                                                    item.Descuento.ToString(),
                                                    item.PuntosCanje.ToString(),
                                                    item.Puntos.ToString(),
                                                    item.Estado.ToString(),
                                                    item.ListaPrecioId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarListaPrecio(int pListaPrecioId, int pArticuloId, decimal pPrecio, decimal pDescuento, int? pPuntos, int? pPuntosCanje, bool pActivo)
        {
            var oprecio = new ListaPrecio();
            oprecio.ListaPrecioId = pListaPrecioId;
            oprecio.ArticuloId = pArticuloId;
            oprecio.Monto = pPrecio;
            oprecio.Descuento = pDescuento;
            oprecio.Puntos = pPuntos;
            oprecio.PuntosCanje = pPuntosCanje;
            oprecio.Estado = pActivo;

            if (pListaPrecioId == 0)
                ListaPrecioBL.Crear(oprecio);
            else
                ListaPrecioBL.Actualizar(oprecio);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public JsonResult BuscarArticulo(string pClave, int maxRows)
        {
            var olista = new SelectList(ArticuloBL.Listar(x => x.Estado && x.Denominacion.Contains(pClave)), "ArticuloId", "Denominacion");
            return Json(olista, JsonRequestBehavior.AllowGet);
        }

        public JsonResult BuscarListaPrecio(int pArticuloId, string pSerie)
        {
            string ArticuloDesc = string.Empty;
            pSerie = pSerie.Trim();
            if (pArticuloId == 0)
            {
                var encontrado = SerieArticuloBL.Listar(x => x.NumeroSerie == pSerie, includeProperties: "Articulo").FirstOrDefault();
                if (encontrado != null)
                {
                    pArticuloId = encontrado.ArticuloId;
                    ArticuloDesc = encontrado.Articulo.Denominacion;
                }

            }



            if (pArticuloId > 0)
            {
                var lp = ListaPrecioBL.Listar(x => x.ArticuloId == pArticuloId, includeProperties: "Articulo")
                            .Select(y => new { y.ArticuloId, y.Articulo.Denominacion, y.Articulo.IndCanjeable, y.ListaPrecioId, y.Monto, y.Descuento, y.Puntos, y.PuntosCanje, y.Estado })
                            .FirstOrDefault();
                if (lp != null)
                    return Json(lp, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    ArticuloId = pArticuloId,
                    Denominacion = ArticuloDesc,
                    ListaPrecioId = 0,
                    Monto = 0.0,
                    Descuento = 0.0,
                    Estado = true
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var oprecio = ListaPrecioBL.Obtener(pid);
            oprecio.Estado = !oprecio.Estado;
            ListaPrecioBL.Actualizar(oprecio);
            return Json(true);
        }

        [HttpPost]
        public ActionResult VerificarIndCanj(int pArticuloId)
        {
            var indCanjeable = ArticuloBL.Obtener(x => x.ArticuloId == pArticuloId).IndCanjeable;
            return Json(indCanjeable);
        }
    }
}
