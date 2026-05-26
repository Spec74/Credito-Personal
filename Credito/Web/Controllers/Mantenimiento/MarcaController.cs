using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;


namespace VendixWeb.Controllers
{
    public class MarcaController : Controller
    {
        
        public ActionResult Index()
        {
           return View();
        }

        public ActionResult ListarMarca(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = MarcaBL.LstMarcaJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.MarcaId,
                            cell = new string[] { 
                                                    item.MarcaId.ToString(),
                                                    item.Denominacion,
                                                    item.Estado.ToString(),
                                                    item.MarcaId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarMarca(int pMarcaId, string pDenominacion, bool pActivo)
        {
            var omarca = new Marca();
            omarca.MarcaId = pMarcaId;
            omarca.Denominacion = pDenominacion;
            omarca.Estado = pActivo;

            if (pMarcaId == 0)
                MarcaBL.Crear(omarca);
            else
                MarcaBL.Actualizar(omarca);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var omarca = MarcaBL.Obtener(pid);
            omarca.Estado = !omarca.Estado;
            MarcaBL.Actualizar(omarca);
            return Json(true);
        }
    }
}
