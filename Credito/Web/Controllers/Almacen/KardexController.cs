using System.Web.Mvc;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers
{
    public class KardexController : Controller
    {
        
        public ActionResult Index()
        {
          //if (Session["UsuarioId"] == null) return RedirectToAction("Login", "Home");
          //var xxx=  AlmacenBL.Listar(x => x.Estado);
          //ViewData["list"] = xxx;
          ViewBag.cboAlmacen = new SelectList(AlmacenBL.Listar(x => x.Estado), "AlmacenId","Denominacion");
          return View();
        }


        public ActionResult ListarKardex(int pArticuloId, int pAlmacenId)
        {
            ViewBag.Articulo = ArticuloBL.Obtener(pArticuloId).Denominacion;
            ViewBag.Almacen = AlmacenBL.Obtener(pAlmacenId).Denominacion;
            var kardex = AlmacenBL.GenerarKardex(pArticuloId, pAlmacenId);
            return PartialView("_kardex", kardex);
        }

        public ActionResult ObtenerSerieKardex(int pMovimientoDetalleId, bool pIndStock)
        {
            var kardex = AlmacenBL.ObtenerSerieKardex(pMovimientoDetalleId, pIndStock);
            return Json(kardex, JsonRequestBehavior.AllowGet);
        }


       
    }
}
