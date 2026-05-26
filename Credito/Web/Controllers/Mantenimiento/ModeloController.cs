using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers
{
    public class ModeloController : Controller
    {
        
        public ActionResult Index()
        {
           
            ViewBag.cboMarca = new SelectList(MarcaBL.Listar(x => x.Estado), "MarcaId", "Denominacion");
            return View();
        }
        public ActionResult ListarModelo(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = ModeloBL.LstModeloJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.ModeloId,
                            cell = new string[] { 
                                                    item.ModeloId.ToString(),
                                                    item.Denominacion,
                                                    item.MarcaId.ToString(),
                                                    item.Marca.Denominacion,
                                                    item.Estado.ToString(),
                                                    item.ModeloId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarModelo(int pModeloId,int pMarcaId, string pDenominacion, string pDescripcion, bool pActivo)
        {
            var item = new Modelo
                           {ModeloId = pModeloId, MarcaId = pMarcaId, Denominacion = pDenominacion, Estado = pActivo};

            if (pModeloId == 0)
                ModeloBL.Crear(item);
            else
                ModeloBL.Actualizar(item);

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var item = ModeloBL.Obtener(pid);
            item.Estado = !item.Estado;
            ModeloBL.Actualizar(item);
            return Json(true);
        }

        public ActionResult CargarComboMarca()
        {
            var xxx = MarcaBL.Listar(x => x.Estado).Select(c => new {Id = c.MarcaId, Valor = c.Denominacion});
            return Json(xxx, JsonRequestBehavior.AllowGet);
        }
    }
}
