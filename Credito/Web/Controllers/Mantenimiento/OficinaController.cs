using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;
using Helper;

namespace VendixWeb.Controllers
{
    [Autenticado]
    public class OficinaController : Controller
    {
       
       public ActionResult Index()
        {
            ViewBag.cboResponsable = new SelectList(UsuarioBL.Listar(x => x.Estado), "UsuarioId", "NombreUsuario");
            return View();
        }
        public ActionResult ListarOficina(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = OficinaBL.LstOficinaJGrid(request, ref totalRecords);
            
            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.OficinaId,
                            cell = new string[] { 
                                                    item.OficinaId.ToString(),
                                                    item.Denominacion,
                                                    item.Descripcion,
                                                    item.Telefono,
                                                    item.UsuarioAsignadoId.ToString(),                                   
                                                    item.IndPrincipal ? "SI":"NO",
                                                    item.Estado.ToString(),
                                                    item.OficinaId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarOficina(int pOficinaId, string pDenominacion, string pDescripcion, string pTelefono,int pUsuarioAsignadoId, bool pActivo, bool pPrincipal)
        {
            var item = new Oficina();
            item.OficinaId = pOficinaId;
            item.Denominacion = pDenominacion;
            item.Descripcion = pDescripcion;
            item.Telefono = pTelefono;
            item.Estado = pActivo;
            item.IndPrincipal = pPrincipal;
            item.UsuarioAsignadoId = pUsuarioAsignadoId;



            if (pOficinaId == 0)
                OficinaBL.CrearOficina(item);
            else
                OficinaBL.Actualizar(item);

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var item = OficinaBL.Obtener(pid);
            item.Estado = !item.Estado;
            OficinaBL.Actualizar(item);
            return Json(true);
        }
    }
}
