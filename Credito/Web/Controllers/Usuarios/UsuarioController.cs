using System;
using System.Linq;
using System.Web.Mvc;
using Helper;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;

namespace VendixWeb.Controllers
{
    [Autenticado]
    public class UsuarioController : Controller
    {

        public ActionResult Index()
        {
            ViewBag.cboOficina = new SelectList(OficinaBL.Listar(x => x.Estado), "OficinaId", "Denominacion");
            return View();
        }
        public ActionResult ListarUsuario(GridDataRequest request)
        {
            int totalRecords = 0;
            var lstGrd = UsuarioBL.LstUsuarioJGrid(request, ref totalRecords);

            var productsData = new
            {
                total = (int)Math.Ceiling((float)totalRecords / (float)request.rows),
                page = request.page,
                records = totalRecords,
                rows = (from item in lstGrd
                        select new
                        {
                            id = item.UsuarioId,
                            cell = new string[] {
                                                    item.UsuarioId.ToString(),
                                                    item.NombreUsuario,
                                                    item.Persona.NombreCompleto,
                                                    item.Persona.Celular1,
                                                    item.Persona.EmailPersonal,
                                                    item.Estado.ToString(),
                                                    item.UsuarioId.ToString() + "," + (item.Estado ? "1":"0")
                                                }
                        }
                       ).ToArray()
            };
            return Json(productsData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarUsuario(int pUsuarioId, string pApePaterno, string pApeMaterno, string pNombre,
                                           string pNumeroDocumento, string pSexoM, DateTime? pFechaNacimiento, string pTelefonoMovil,
                                           string pEmailPersonal, string pDireccion, string pNombreUsuario, string pClaveUsuario, bool pActivo)
        {
            var user = new Usuario();
            var perso = PersonaBL.Obtener(x => x.NumeroDocumento == pNumeroDocumento);
            if (perso==null)
                perso= new Persona();  
           
            pApePaterno = pApePaterno.ToUpper();
            pApeMaterno = pApeMaterno.ToUpper();
            pNombreUsuario = pNombreUsuario.ToUpper();

            perso.ApePaterno = pApePaterno;
            perso.ApeMaterno = pApeMaterno;
            perso.Nombre = pNombre;
            perso.NombreCompleto = pApePaterno + " " + pApeMaterno + ", " + pNombre;
            perso.NumeroDocumento = pNumeroDocumento;
            perso.Sexo = pSexoM;
            perso.FechaNacimiento = pFechaNacimiento;
            perso.Celular1 = pTelefonoMovil;
            perso.EmailPersonal = pEmailPersonal;
            perso.Direccion = pDireccion;
            perso.TipoDocumento = "DNI";
            perso.TipoPersona = "N";
            perso.Estado = pActivo;

            if (perso.PersonaId == 0)
                PersonaBL.Crear(perso);
            else
                PersonaBL.Actualizar(perso);


            if (pUsuarioId > 0)            
                user = UsuarioBL.Obtener(pUsuarioId);
            
            user.PersonaId = perso.PersonaId;
            user.UsuarioId = pUsuarioId;
            user.NombreUsuario = pNombreUsuario;
            user.ClaveUsuario = pClaveUsuario;
            user.Estado = pActivo;
            if (pUsuarioId == 0)
                UsuarioBL.Crear(user);
            else
                UsuarioBL.Actualizar(user);

            return Json(user.UsuarioId, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerUsuarioPersona(int pUsuarioId)
        {
            var user = UsuarioBL.Obtener(pUsuarioId);
            var persona = PersonaBL.Obtener(user.PersonaId);
            var oficinas = (from of in OficinaBL.Listar(x => x.Estado)
                            join us in UsuarioOficinaBL.Listar(x => x.Estado && x.UsuarioId == pUsuarioId) on of.OficinaId equals
                                us.OficinaId into factDesc
                            from fd in factDesc.DefaultIfEmpty()
                            select new
                            {
                                of.OficinaId,
                                of.Denominacion,
                                Asignado = (fd == null) ? 0 : 1
                            }
                            ).ToList();


            return Json(new
            {
                Usuario = user,
                Persona = PersonaBL.Obtener(user.PersonaId),
                FNacimiento = persona.FechaNacimiento != null ? persona.FechaNacimiento.Value.ToShortDateString() : String.Empty,
                Oficinas = oficinas
            }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult ResetearClave(int pUsuarioId)
        {
            var ousuario = UsuarioBL.Obtener(pUsuarioId);
            ousuario.ClaveUsuario = "123456";
            UsuarioBL.Actualizar(ousuario);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Activar(int pid)
        {
            var ousuario = UsuarioBL.Obtener(pid);
            ousuario.Estado = !ousuario.Estado;
            UsuarioBL.Actualizar(ousuario);
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AsignarOficina(int pUsuarioId, string pOficinas)
        {
            var lst = pOficinas.Split(',');
            UsuarioOficinaBL.EjecutarSql("DELETE FROM MAESTRO.UsuarioOficina WHERE UsuarioId=" + pUsuarioId.ToString());
            foreach (var item in lst)
            {
                UsuarioOficinaBL.Crear(new UsuarioOficina()
                { UsuarioId = pUsuarioId, OficinaId = int.Parse(item), Estado = true });
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AsignarRol(int pUsuarioId, int pOficinaId, string pRoles)
        {
            var lst = pRoles.Split(',');
            UsuarioRolBL.EjecutarSql("DELETE FROM MAESTRO.UsuarioRol WHERE UsuarioId=" + pUsuarioId.ToString() + " and OficinaId=" + pOficinaId.ToString());
            foreach (var item in lst)
            {
                UsuarioRolBL.Crear(new UsuarioRol() { UsuarioId = pUsuarioId, OficinaId = pOficinaId, RolId = int.Parse(item) });
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListarOficinas()
        {
            return Json(OficinaBL.Listar(x => x.Estado).Select(s => new { s.OficinaId, s.Denominacion }),
                        JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerUsuarioRol(int? pOficinaId, int pUsuarioId)
        {
            if (!pOficinaId.HasValue)
                return Json(null, JsonRequestBehavior.AllowGet);

            var roles = (from of in RolBL.Listar(x=>x.Estado)
                         join us in UsuarioRolBL.Listar(x => x.UsuarioId == pUsuarioId && x.OficinaId == pOficinaId) on of.RolId equals us.RolId into factDesc
                         from fd in factDesc.DefaultIfEmpty()
                         select new
                         {
                             of.RolId,
                             of.Denominacion,
                             Asignado = (fd == null) ? 0 : 1
                         }
                        ).ToList();

            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerPersonaDNI(string pDNI)
        {
            var persona = PersonaBL.Obtener(x => x.NumeroDocumento == pDNI);

            if (persona == null)
                return Json(null, JsonRequestBehavior.AllowGet);


            return Json(new
            {
                Persona = persona,
                FNacimiento = persona.FechaNacimiento != null ? persona.FechaNacimiento.Value.ToShortDateString() : String.Empty,
            }
                , JsonRequestBehavior.AllowGet);
        }
        public ActionResult ValidarUsuarioDNI(string pDNI)
        {
            var users = UsuarioBL.Contar(x => x.Persona.NumeroDocumento == pDNI, includeProperties: "Persona");
            if (users > 0)
                return Json(true, JsonRequestBehavior.AllowGet);

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        
    }
}
