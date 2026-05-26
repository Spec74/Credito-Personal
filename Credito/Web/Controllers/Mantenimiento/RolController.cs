using System;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;
using Helper;

namespace VendixWeb.Controllers.Mantenimiento
{
    [Autenticado]
    public class RolController : Controller
    {
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ListarRolJgrid(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = RolBL.LstRolJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.RolId,
                            cell = new string[] { 
                                                    item.RolId.ToString(),
                                                    item.Denominacion,
                                                    item.Estado.ToString(),
                                                    item.RolId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var obj = RolBL.Obtener(pid);
            obj.Estado = !obj.Estado;
            RolBL.Actualizar(obj);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarRol(int pRolId, string pDenominacion, bool pActivo)
        {
            var perso = new Rol();
            perso.RolId = pRolId;
            perso.Denominacion = pDenominacion.ToUpper();
            perso.Estado = pActivo;

            if (pRolId == 0)
                RolBL.Crear(perso);
            else
                RolBL.Actualizar(perso);
            return Json(perso.RolId, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ListarMenu()
        {
            return Json(MenuBL.Listar().Select(x => new { x.MenuId, Denominacion = x.Modulo + " - " + x.Denominacion }).OrderBy(x=>x.Denominacion),
                        JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Asignar(int pRolId, string pRoles)
        {
            RolMenuBL.EjecutarSql("DELETE FROM MAESTRO.RolMenu WHERE RolId=" + pRolId.ToString());
            if (!string.IsNullOrEmpty(pRoles))
            {
                var lst = pRoles.Split(',');
                foreach (var item in lst)
                {
                    RolMenuBL.Crear(new RolMenu() { RolId = pRolId, MenuId = int.Parse(item) });
                }
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerRolMenu(int pRolId)
        {
            var rol = RolBL.Obtener(pRolId);
            var menus = (from of in MenuBL.Listar(x=>x.IndPadre.Value==false)
                         join us in RolMenuBL.Listar(x => x.RolId == pRolId) on of.MenuId equals us.MenuId into factDesc
                         from fd in factDesc.DefaultIfEmpty()
                         select new
                                    {
                                        of.MenuId,
                                        Denominacion = of.Modulo + " - " + of.Denominacion ,
                                        Asignado = (fd == null) ? 0 : 1
                                    }
                        ).ToList();

            return Json(new
            {
                Rol = rol,
                Menus = menus.OrderBy(x=>x.Denominacion)
            }, JsonRequestBehavior.AllowGet);
        }

    }
}
