using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ITB.VENDIX.BL;
using ITB.VENDIX.DA;
using Helper;
using Web.Models;

namespace VendixWeb.Controllers
{

    public class HomeController : Controller
    {
        [Autenticado]
        public ActionResult Index()
        {

            return View("Index");
        }
        
        public ActionResult Login()
        {
            //if (id!= ConfigurationManager.AppSettings["tk"].ToString())
            //    return Content("No Autorizado");
            Session["UsuarioId"] = 1;
            ViewBag.cboOficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion");            
            //ViewBag.tk = id;
            return View("Login");
        }        
      
        public ActionResult Autenticar(string login_name, string login_pw, string tk )
        {
            var rm = new Web.Models.ResponseModel();

            var acceso = AccesoBL.Contar(x => x.DireccionIp == tk);
            if (acceso == 0) {
                rm.SetResponse(false, "Acceso No Autorizado");
                return Json(rm);
            }          

            int oficinaId = int.Parse(Request.Form["cboOficina"]);
            var usuarioOficina = UsuarioOficinaBL.Listar(x => x.Usuario.NombreUsuario == login_name && x.Usuario.ClaveUsuario == login_pw
                                         && x.OficinaId == oficinaId && x.Estado && x.Usuario.Estado, null, "Usuario,Oficina").FirstOrDefault();
            if (usuarioOficina != null)
            {
                SessionHelper.AddUserToSession(usuarioOficina.UsuarioId.ToString());
                rm.SetResponse(true);
                rm.href = Url.Action("Index", "Home");

                VendixGlobal<int>.Crear("UsuarioOficinaId", usuarioOficina.UsuarioOficinaId);

                //usuario asginado a oficina
                var usuarioAsignadoId = OficinaBL.Obtener(x => x.OficinaId == usuarioOficina.OficinaId && x.Estado).UsuarioAsignadoId;

                VendixGlobal<int>.Crear("UsuarioIdAsignadoOficina", usuarioAsignadoId);

                VendixGlobal<int>.Crear("UsuarioId", usuarioOficina.UsuarioId);
                VendixGlobal<string>.Crear("NombreUsuario", usuarioOficina.Usuario.NombreUsuario);
                VendixGlobal<string>.Crear("NombreOficina", usuarioOficina.Oficina.Denominacion);
                VendixGlobal<int>.Crear("OficinaId", usuarioOficina.OficinaId);

                VendixGlobal<int>.Crear("BovedaId", BovedaBL.Listar(x => x.OficinaId == oficinaId && x.IndCierre == false
                                                                && x.IndTemporal == false).FirstOrDefault().BovedaId);


                //var Bovedatmp = BovedaBL.Obtener(x => x.OficinaId == oficinaId && x.IndCierre == false && x.IndTemporal == true);
                //if (Bovedatmp == null)
                //    VendixGlobal<int>.Crear("BovedaId", BovedaBL.Listar(x => x.OficinaId == oficinaId && x.IndCierre == false
                //                                                && x.IndTemporal == false).FirstOrDefault().BovedaId);
                //else 
                //    VendixGlobal<int>.Crear("BovedaId", Bovedatmp.BovedaId);
                
                VendixGlobal<List<usp_MenuLst_Result>>.Crear("Menu", MenuBL.ListaMenuDinamico());

                //System.Web.HttpContext.Current.Cache.Insert("Menu", MenuBL.ListaMenuDinamico());
                //var x = HttpRuntime.Cache.Get("Menu") as List<usp_MenuLst_Result>;

                //return RedirectToAction("Index");
            }
            else {
                rm.SetResponse(false, "Usuario o Clave Incorrecta");
            }
            return Json(rm);
            //return RedirectToAction("Login", new { id = tk, mensaje = "Usuario o Clave Incorrecto" });
        }
        
        public ActionResult LogOff()
        {
            SessionHelper.DestroyUserSession();
            VendixGlobal<int>.EliminarTodo();
            return RedirectToAction("Login", "Home");
        }
       
        public ActionResult ListarOficina()
        {
            return Json(new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion"), JsonRequestBehavior.AllowGet);
        }
        public ActionResult ConfirmarClave(string clave)
        {
            var rpta = new Respuesta() { Error = false };
            var admin = UsuarioBL.Contar(x => x.ClaveUsuario == clave && x.NombreUsuario == "ADMVENDIX");
            if (admin <= 0)
            {
                rpta.Error = true;
                rpta.Mensaje = "NO AUTORIZADO!!!!";
                return Json(rpta, JsonRequestBehavior.AllowGet);
            }

            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
        public ActionResult CrearAcceso(string pDireccion)
        {
            var rpta = new Respuesta() { Error = false };
            try
            {
                
                var ip = AccesoBL.Obtener(x => x.DireccionIp == pDireccion);
                if (ip == null)
                    AccesoBL.Crear(new Acceso { DireccionIp = pDireccion });
            }
            catch (System.Exception ex)
            {
                rpta.Error = true;
                rpta.Mensaje = ex.Message;                
            }          

            return Json(rpta, JsonRequestBehavior.AllowGet);
        }
    }
}

