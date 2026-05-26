using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers.Venta
{
    public class CanjearPuntosController : Controller
    {
      //
            // GET: /CanjearPuntos/

            public ActionResult Index()
            {
                return View();
            }

            public JsonResult ObtenerPuntos(int pPersonaId)
            {
                var obj = TarjetaPuntoBL.Obtener(x => x.PersonaId == pPersonaId && x.Estado);
                return Json(obj, JsonRequestBehavior.AllowGet);
            }

            public JsonResult CanjearArticulo(int pCodCliente, string pNumSerie)
            {

                string cad = TarjetaPuntoBL.CanjearPuntos(pCodCliente, pNumSerie);

                return Json(cad, JsonRequestBehavior.AllowGet);
            }

            public ActionResult Listar(GridDataRequest request)
            {
                int totalRecords = 0;
                var lstGrd = ArticuloBL.LstListaArticulosJGrid(request, ref totalRecords);

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
                                                    item.PuntosCanje.ToString(),
                                                    item.Estado.ToString(),
                                                    item.ListaPrecioId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                            }
                           ).ToArray()
                };
                return Json(productsData, JsonRequestBehavior.AllowGet);
            }
        }
    }
