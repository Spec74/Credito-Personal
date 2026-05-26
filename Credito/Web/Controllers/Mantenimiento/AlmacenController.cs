using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;
namespace VendixWeb.Controllers
{
    public class AlmacenController : Controller
    {
        public ActionResult Index()
        {
           // if (Session["UsuarioId"] == null) return RedirectToAction("Login", "Home");

            ViewBag.cboOficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion");
            return View();
        }
        public ActionResult ListarAlmacen(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstItem = AlmacenBL.LstAlmacenJGrid(request, ref totalRecords);
            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstItem
                        select new
                        {
                            id = item.AlmacenId,
                            cell = new string[] { 
                                                    item.AlmacenId.ToString(),
                                                    item.Denominacion,
                                                    item.Descripcion,
                                                    item.OficinaId.ToString(),
                                                    item.Oficina.Denominacion,
                                                    item.Estado.ToString(),
                                                    item.AlmacenId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarAlmacen(int pAlmacenId ,int pOficinaId, string pDenominacion, string pDescripcion, bool pActivo)
        {
            var item = new ITB.VENDIX.DA.Almacen();
            item.AlmacenId = pAlmacenId;
            item.OficinaId = pOficinaId;
            item.Denominacion = pDenominacion;
            item.Descripcion = pDescripcion;
            item.Estado = pActivo;

            //if (pAlmacenId == 0)
            //    AlmacenBL.Crear(item);
            //else
            //    AlmacenBL.Actualizar(item);

            AlmacenBL.Guardar(item);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var item = AlmacenBL.Obtener(pid);
            item.Estado = !item.Estado;
            //AlmacenBL.Actualizar(item);
            AlmacenBL.ActualizarParcial(item, x=>x.Estado);
            return Json(true);
        }

        public ActionResult CargarComboMarca()
        {
            var xxx = OficinaBL.Listar(x => x.Estado).Select(c => new { Id = c.OficinaId, Valor = c.Denominacion });
            return Json(xxx, JsonRequestBehavior.AllowGet);
        }

    }
}
