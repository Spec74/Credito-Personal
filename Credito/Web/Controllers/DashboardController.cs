using Helper;
using ITB.VENDIX.BL;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.SessionState;

namespace VendixWeb.Controllers
{
    [Autenticado]
    [SessionState(SessionStateBehavior.ReadOnly)]
    public class DashboardController : Controller
    {
        private const int ROL_ADMINISTRADOR = 1;
        private const int ROL_ANALISTA = 6;

        public ActionResult Admin()
        {
            if (ObtenerRolId() != ROL_ADMINISTRADOR)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public ActionResult Gestor()
        {
            if (ObtenerRolId() != ROL_ANALISTA)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public JsonResult ObtenerAdminResumen()
        {
            if (ObtenerRolId() != ROL_ADMINISTRADOR)
            {
                return JsonSinPermisoGerencial();
            }

            try
            {
                var datos = DashboardBL.ObtenerAdminResumen(null);
                return JsonExito(datos);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerAdminAnalistas()
        {
            if (ObtenerRolId() != ROL_ADMINISTRADOR)
            {
                return JsonSinPermisoGerencial();
            }

            try
            {
                var datos = DashboardBL.ObtenerAdminAnalistas(null);
                return JsonExito(datos);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerAdminFlujoCaja()
        {
            if (ObtenerRolId() != ROL_ADMINISTRADOR)
            {
                return JsonSinPermisoGerencial();
            }

            try
            {
                var datos = DashboardBL.ObtenerAdminFlujoCaja();
                return JsonExito(datos);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerAdminHistorico()
        {
            if (ObtenerRolId() != ROL_ADMINISTRADOR)
            {
                return JsonSinPermisoGerencial();
            }

            try
            {
                var datos = DashboardBL.ObtenerAdminHistorico(null, 30);
                return JsonExito(datos);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerAdminHistoricoMensual()
        {
            if (ObtenerRolId() != ROL_ADMINISTRADOR)
            {
                return JsonSinPermisoGerencial();
            }

            try
            {
                var datos = DashboardBL.ObtenerAdminHistoricoMensual(null, 12);
                return JsonExito(datos);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerDashboard()
        {
            if (ObtenerRolId() != ROL_ANALISTA)
            {
                return Json(new
                {
                    Error = true,
                    Mensaje = "No tiene permisos para consultar el dashboard del analista."
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var dashboard = DashboardBL.ObtenerDashboard();
                string nombreGestor =
                    VendixGlobal<string>.Obtener("NombreCompletoAsesor");

                return Json(new
                {
                    Error = false,
                    NombreGestor = nombreGestor,
                    Data = dashboard
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerGrafico()
        {
            if (ObtenerRolId() != ROL_ANALISTA)
            {
                return Json(new
                {
                    Error = true,
                    Mensaje = "No tiene permisos para consultar la productividad del analista."
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                /*
                   El contrato Anio/Mes/NombreMes/MontoCobrado se conserva.
                   NombreMes contiene ahora la etiqueta diaria dd/MM, por lo
                   que no es necesario cambiar DashboardBL ni el Complex Type
                   de usp_DashboardProductividad.
                */
                var resultado = DashboardBL.ObtenerProductividad()
                    .Select(x => new
                    {
                        Anio = x.Anio,
                        Mes = x.Mes,
                        NombreMes = x.NombreMes,
                        MontoCobrado = x.MontoCobrado
                    })
                    .ToList();

                return JsonExito(resultado);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        public JsonResult ObtenerClientesMora(string tipo)
        {
            if (ObtenerRolId() != ROL_ANALISTA)
            {
                return Json(new
                {
                    Error = true,
                    Mensaje = "No tiene permisos para consultar la cartera morosa del analista."
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var resultado = DashboardBL.ObtenerClientesMora(tipo)
                    .Select(x => new
                    {
                        PersonaId = x.PersonaId,
                        NombreCompleto = x.NombreCompleto,
                        CreditosMora = x.CreditosMora,
                        SaldoMora = x.SaldoMora,
                        PrimeraCuotaVencida = x.PrimeraCuotaVencida,
                        FechaUltimoPago = x.FechaUltimoPago,
                        DiasAtraso = x.DiasAtraso,
                        CodigoClasificacion = x.CodigoClasificacion,
                        Clasificacion = x.Clasificacion
                    })
                    .ToList();

                return JsonExito(resultado);
            }
            catch (Exception ex)
            {
                return JsonError(ex);
            }
        }

        /*
           ObtenerRanking y ObtenerTopAnterior fueron retirados porque el
           dashboard del analista ya no realiza comparaciones entre personas.
           Los SP legacy pueden conservarse temporalmente hasta comprobar que
           ningún otro módulo los consume.
        */

        private static int ObtenerRolId()
        {
            return VendixGlobal<int>.Obtener("RolId");
        }

        private JsonResult JsonSinPermisoGerencial()
        {
            return Json(new
            {
                Error = true,
                Mensaje = "No tiene permisos para consultar el tablero gerencial."
            }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonExito(object datos)
        {
            return Json(new
            {
                Error = false,
                Data = datos
            }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonError(Exception ex)
        {
            Exception error = ex;

            while (error.InnerException != null)
            {
                error = error.InnerException;
            }

            return Json(new
            {
                Error = true,
                Mensaje = error.Message
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
