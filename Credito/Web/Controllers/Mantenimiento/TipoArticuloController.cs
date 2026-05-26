using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers.Mantenimiento
{
    public class TipoArticuloController : Controller
    {
       public ActionResult Index()
        {
            return View();
        }
        public ActionResult ListarTipoArticulo(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = TipoArticuloBL.LstTipoArticuloJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.TipoArticuloId,
                            cell = new string[] { 
                                                    item.TipoArticuloId.ToString(),
                                                    item.Denominacion,
                                                    item.Descripcion,
                                                    item.Estado.ToString(),
                                                    item.TipoArticuloId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarTipoArticulo(int pTipoArticuloId, string pDenominacion, string pDescripcion, bool pActivo)
        {
            var item = new TipoArticulo();
            item.TipoArticuloId = pTipoArticuloId;
            item.Denominacion = pDenominacion;
            item.Descripcion = pDescripcion;
            item.Estado = pActivo;

            if (pTipoArticuloId == 0)
                TipoArticuloBL.Crear(item);
            else
                TipoArticuloBL.Actualizar(item);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var item = TipoArticuloBL.Obtener(pid);
            item.Estado = !item.Estado;
            TipoArticuloBL.Actualizar(item);
            return Json(true);
        }
    }
}
