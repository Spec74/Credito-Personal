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
        private const int ROL_ADMINISTRADOR = 1;
        private const int ROL_ANALISTA = 6;

        [Autenticado]
        public ActionResult Index()
        {
            int rolId = VendixGlobal<int>.Obtener("RolId");

            // ANALISTA
            if (rolId == ROL_ANALISTA)
            {
                return RedirectToAction("Gestor", "Dashboard");
            }

            // ADMINISTRADOR
            //if (rolId == ROL_ADMINISTRADOR)
            //{
            //    return RedirectToAction("Admin", "Dashboard");
            //}

            // CAJA y demás perfiles continúan utilizando
            // el Index tradicional.
            return View("Index");
        }

        public ActionResult Login()
        {
            // Actualmente se mantiene porque forma parte
            // del comportamiento existente del sistema.
            Session["UsuarioId"] = 1;

            ViewBag.cboOficina = new SelectList(
                OficinaBL.Listar(x => x.Estado),
                "OficinaId",
                "Denominacion"
            );

            return View("Login");
        }

        public ActionResult Autenticar(
            string login_name,
            string login_pw,
            string tk)
        {
            var rm = new ResponseModel();

            try
            {
                //----------------------------------------------------------
                // 1. VALIDAR ACCESO POR TOKEN / DIRECCIÓN AUTORIZADA
                //----------------------------------------------------------

                var acceso = AccesoBL.Contar(
                    x => x.DireccionIp == tk
                );

                if (acceso == 0)
                {
                    rm.SetResponse(
                        false,
                        "Acceso No Autorizado"
                    );

                    return Json(rm);
                }

                //----------------------------------------------------------
                // 2. VALIDAR OFICINA
                //----------------------------------------------------------

                int oficinaId;

                if (!int.TryParse(
                        Request.Form["cboOficina"],
                        out oficinaId))
                {
                    rm.SetResponse(
                        false,
                        "La oficina seleccionada no es válida."
                    );

                    return Json(rm);
                }

                //----------------------------------------------------------
                // 3. AUTENTICAR USUARIO
                //----------------------------------------------------------

                var usuarioOficina =
                    UsuarioOficinaBL.Listar(
                        x =>
                            x.Usuario.NombreUsuario == login_name &&
                            x.Usuario.ClaveUsuario == login_pw &&
                            x.OficinaId == oficinaId &&
                            x.Estado &&
                            x.Usuario.Estado,
                        null,
                        "Usuario,Usuario.Persona,Oficina"
                    )
                    .FirstOrDefault();

                if (usuarioOficina == null)
                {
                    rm.SetResponse(
                        false,
                        "Usuario o Clave Incorrecta"
                    );

                    return Json(rm);
                }

                //----------------------------------------------------------
                // 4. CREAR SESIÓN
                //----------------------------------------------------------

                SessionHelper.AddUserToSession(
                    usuarioOficina.UsuarioId.ToString()
                );

                //----------------------------------------------------------
                // 5. DATOS PRINCIPALES DEL USUARIO
                //----------------------------------------------------------

                VendixGlobal<int>.Crear(
                    "UsuarioOficinaId",
                    usuarioOficina.UsuarioOficinaId
                );

                VendixGlobal<int>.Crear(
                    "UsuarioId",
                    usuarioOficina.UsuarioId
                );

                VendixGlobal<string>.Crear(
                    "NombreUsuario",
                    usuarioOficina.Usuario.NombreUsuario
                );

                VendixGlobal<string>.Crear(
                    "NombreOficina",
                    usuarioOficina.Oficina.Denominacion
                );

                VendixGlobal<int>.Crear(
                    "OficinaId",
                    usuarioOficina.OficinaId
                );

                //----------------------------------------------------------
                // 6. NOMBRE COMPLETO DEL ANALISTA / USUARIO
                //----------------------------------------------------------

                string nombreReal =
                    usuarioOficina.Usuario.Persona != null
                        ? usuarioOficina.Usuario.Persona.NombreCompleto
                        : usuarioOficina.Usuario.NombreUsuario;

                VendixGlobal<string>.Crear(
                    "NombreCompletoAsesor",
                    nombreReal
                );

                //----------------------------------------------------------
                // 7. ROL DEL USUARIO
                //----------------------------------------------------------

                var rolesUsuario =
                    UsuarioRolBL.Listar(
                        x =>
                            x.UsuarioId == usuarioOficina.UsuarioId &&
                            x.OficinaId == usuarioOficina.OficinaId
                    );

                int rolId = 0;

                /*
                 * Prioridad:
                 *
                 * ADMINISTRADOR > ANALISTA > OTROS
                 *
                 * Si accidentalmente un usuario tiene más de un rol,
                 * ADMINISTRADOR tendrá prioridad.
                 */
                if (rolesUsuario.Any(
                    x => x.RolId == ROL_ADMINISTRADOR))
                {
                    rolId = ROL_ADMINISTRADOR;
                }
                else if (rolesUsuario.Any(
                    x => x.RolId == ROL_ANALISTA))
                {
                    rolId = ROL_ANALISTA;
                }
                else
                {
                    var primerRol = rolesUsuario.FirstOrDefault();

                    if (primerRol != null)
                    {
                        rolId = primerRol.RolId ?? 0;
                    }
                }

                VendixGlobal<int>.Crear("RolId", rolId);

                //----------------------------------------------------------
                // 8. USUARIO ASIGNADO A LA OFICINA
                //----------------------------------------------------------

                var oficina =
                    OficinaBL.Obtener(
                        x =>
                            x.OficinaId ==
                                usuarioOficina.OficinaId &&
                            x.Estado
                    );

                if (oficina != null)
                {
                    VendixGlobal<int>.Crear(
                        "UsuarioIdAsignadoOficina",
                        oficina.UsuarioAsignadoId
                    );
                }

                //----------------------------------------------------------
                // 9. BÓVEDA ACTIVA
                //----------------------------------------------------------

                var bovedaActiva =
                    BovedaBL.Listar(
                        x =>
                            x.OficinaId == oficinaId &&
                            x.IndCierre == false &&
                            x.IndTemporal == false
                    )
                    .FirstOrDefault();

                if (bovedaActiva == null)
                {
                    rm.SetResponse(
                        false,
                        "No existe una bóveda activa para la oficina seleccionada."
                    );

                    SessionHelper.DestroyUserSession();
                    VendixGlobal<int>.EliminarTodo();

                    return Json(rm);
                }

                VendixGlobal<int>.Crear(
                    "BovedaId",
                    bovedaActiva.BovedaId
                );

                //----------------------------------------------------------
                // 10. MENÚ DINÁMICO
                //----------------------------------------------------------

                VendixGlobal<List<usp_MenuLst_Result>>.Crear(
                    "Menu",
                    MenuBL.ListaMenuDinamico()
                );

                //----------------------------------------------------------
                // 11. RESPUESTA DE AUTENTICACIÓN
                //----------------------------------------------------------

                rm.SetResponse(true);

                /*
                 * Siempre enviamos a Home/Index.
                 *
                 * Index determinará automáticamente si:
                 *
                 * ANALISTA → Dashboard/Gestor
                 * ADMIN    → Home/Index
                 */
                rm.href = Url.Action(
                    "Index",
                    "Home"
                );

                return Json(rm);
            }
            catch (System.Exception ex)
            {
                rm.SetResponse(
                    false,
                    "Ocurrió un error durante la autenticación: " +
                    ex.Message
                );

                return Json(rm);
            }
        }

        public ActionResult LogOff()
        {
            SessionHelper.DestroyUserSession();

            VendixGlobal<int>.EliminarTodo();

            return RedirectToAction(
                "Login",
                "Home"
            );
        }

        public ActionResult ListarOficina()
        {
            return Json(
                new SelectList(
                    OficinaBL.Listar(x => x.Estado),
                    "OficinaId",
                    "Denominacion"
                ),
                JsonRequestBehavior.AllowGet
            );
        }

        public ActionResult ConfirmarClave(string clave)
        {
            var rpta = new Respuesta
            {
                Error = false
            };

            var admin =
                UsuarioBL.Contar(
                    x =>
                        x.ClaveUsuario == clave &&
                        x.NombreUsuario == "ADMVENDIX"
                );

            if (admin <= 0)
            {
                rpta.Error = true;
                rpta.Mensaje = "NO AUTORIZADO!!!!";
            }

            return Json(
                rpta,
                JsonRequestBehavior.AllowGet
            );
        }

        public ActionResult CrearAcceso(string pDireccion)
        {
            var rpta = new Respuesta
            {
                Error = false
            };

            try
            {
                var ip =
                    AccesoBL.Obtener(
                        x =>
                            x.DireccionIp == pDireccion
                    );

                if (ip == null)
                {
                    AccesoBL.Crear(
                        new Acceso
                        {
                            DireccionIp = pDireccion
                        }
                    );
                }
            }
            catch (System.Exception ex)
            {
                rpta.Error = true;
                rpta.Mensaje = ex.Message;
            }

            return Json(
                rpta,
                JsonRequestBehavior.AllowGet
            );
        }
    }
}