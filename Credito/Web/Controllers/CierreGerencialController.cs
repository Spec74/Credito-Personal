using ITB.VENDIX.BL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using VendixWeb.Models.CierreGerencial;
using VendixWeb.Services.CierreGerencial;

namespace VendixWeb.Controllers
{
    [Authorize]
    public sealed class CierreGerencialController : Controller
    {
        private readonly ICierreGerencialDataService _datos;
        private readonly ICierreGerencialExcelService _excel;

        public CierreGerencialController()
            : this(
                new CierreGerencialDataService(),
                new CierreGerencialExcelService())
        {
        }

        public CierreGerencialController(ICierreGerencialDataService datos)
            : this(datos, new CierreGerencialExcelService())
        {
        }

        public CierreGerencialController(
            ICierreGerencialDataService datos,
            ICierreGerencialExcelService excel)
        {
            if (datos == null)
                throw new ArgumentNullException("datos");
            if (excel == null)
                throw new ArgumentNullException("excel");

            _datos = datos;
            _excel = excel;
        }

        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var usuarioId = VendixGlobal.GetUsuarioId();

            if (usuarioId <= 0)
            {
                filterContext.Result = new HttpStatusCodeResult(
                    401,
                    "La sesion del usuario ha expirado.");
                return;
            }

            if (!UsuarioPuedeConsultar(usuarioId))
            {
                filterContext.Result = new HttpStatusCodeResult(
                    403,
                    "Su usuario no esta autorizado para consultar el modulo gerencial.");
            }
        }

        [HttpGet]
        public ActionResult Index()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            ViewBag.PuedeGestionarMetas =
                UsuarioPuedeGestionarMetas(usuarioId);

            return View();
        }

        [HttpGet]
        public JsonResult ObtenerAvance(DateTime periodo)
        {
            try
            {
                var oficinaId = VendixGlobal.GetOficinaId();
                var filas = _datos.ObtenerAvance(periodo, oficinaId);

                return Json(new
                {
                    success = true,
                    periodo = PrimerDia(periodo).ToString("yyyy-MM-dd"),
                    fechaCalculo = filas.Any()
                        ? filas[0].FechaCalculo.ToString("yyyy-MM-dd HH:mm:ss")
                        : null,
                    avanceNoOficial = filas.Any() && filas[0].AvanceNoOficial,
                    total = filas.Count,
                    filas
                }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 409;
                return ErrorJson(MensajeSqlSeguro(ex), true);
            }
            catch (ArgumentException ex)
            {
                Response.StatusCode = 400;
                return ErrorJson(ex.Message, true);
            }
        }

        [HttpGet]
        public JsonResult ListarMetas(DateTime periodo)
        {
            try
            {
                var usuarioId = VendixGlobal.GetUsuarioId();
                var metas = _datos.ListarMetas(periodo);

                return Json(new
                {
                    success = true,
                    periodo = PrimerDia(periodo).ToString("yyyy-MM-dd"),
                    periodoCerrado = metas.Any() && metas[0].PeriodoCerrado,
                    puedeEditar =
                        UsuarioPuedeGestionarMetas(usuarioId) &&
                        metas.Any() &&
                        metas[0].PuedeEditar,
                    fechaLimiteEdicion = metas.Any()
                        ? metas[0].FechaLimiteEdicion.ToString("yyyy-MM-dd HH:mm:ss")
                        : null,
                    metas
                }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 409;
                return ErrorJson(MensajeSqlSeguro(ex), true);
            }
        }

        [HttpGet]
        public ActionResult ExportarExcel(DateTime periodo)
        {
            try
            {
                var periodoNormalizado = PrimerDia(periodo);
                var oficinaId = VendixGlobal.GetOficinaId();
                var filas = _datos.ObtenerAvance(
                    periodoNormalizado,
                    oficinaId);

                if (filas == null || filas.Count == 0)
                    return HttpNotFound(
                        "No existen datos gerenciales para el periodo solicitado.");

                var contenido = _excel.Generar(filas);
                var estado = filas[0].AvanceNoOficial
                    ? "AVANCE_NO_OFICIAL"
                    : "CIERRE_OFICIAL";
                var nombre = string.Format(
                    "VENDIX_{0}_{1:yyyy_MM}_{2:yyyyMMdd_HHmm}.xlsx",
                    estado,
                    periodoNormalizado,
                    filas[0].FechaCalculo);

                return File(
                    contenido,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombre);
            }
            catch (SqlException ex)
            {
                return new HttpStatusCodeResult(
                    409,
                    MensajeSqlSeguro(ex));
            }
            catch (ArgumentException ex)
            {
                return new HttpStatusCodeResult(400, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return new HttpStatusCodeResult(409, ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GuardarMetas(
            DateTime periodo,
            IList<MetaGerencialDefinitivaDto> metas)
        {
            try
            {
                var usuarioId = VendixGlobal.GetUsuarioId();

                if (!UsuarioPuedeGestionarMetas(usuarioId))
                {
                    Response.StatusCode = 403;
                    return ErrorJson(
                        "Solo BRIGIDA puede registrar o modificar las metas.",
                        false);
                }

                _datos.GuardarMetas(periodo, metas, usuarioId);

                return Json(new
                {
                    success = true,
                    message = "Las metas fueron guardadas correctamente.",
                    periodo = PrimerDia(periodo).ToString("yyyy-MM-dd"),
                    total = metas == null ? 0 : metas.Count
                });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 409;
                return ErrorJson(MensajeSqlSeguro(ex), false);
            }
            catch (ArgumentException ex)
            {
                Response.StatusCode = 400;
                return ErrorJson(ex.Message, false);
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return ErrorJson(
                    "No se pudieron guardar las metas. Revise el registro tecnico.",
                    false);
            }
        }

        private static JsonResult ErrorJson(
            string mensaje,
            bool permitirGet)
        {
            var resultado = new JsonResult
            {
                Data = new
                {
                    success = false,
                    message = mensaje
                }
            };

            if (permitirGet)
                resultado.JsonRequestBehavior =
                    JsonRequestBehavior.AllowGet;

            return resultado;
        }

        private static string MensajeSqlSeguro(SqlException ex)
        {
            if (ex.Number >= 50100 && ex.Number <= 50699)
                return ex.Message;

            return "La base de datos no pudo procesar la operacion gerencial.";
        }

        private static bool UsuarioPuedeGestionarMetas(int usuarioId)
        {
            return UsuarioEstaConfigurado(
                "CierreGerencial.UsuarioMetasIds",
                "10",
                usuarioId);
        }

        private static bool UsuarioPuedeConsultar(int usuarioId)
        {
            return UsuarioEstaConfigurado(
                "CierreGerencial.UsuarioConsultaIds",
                "3,10",
                usuarioId);
        }

        private static bool UsuarioEstaConfigurado(
            string clave,
            string valorPredeterminado,
            int usuarioId)
        {
            var configurados = ConfigurationManager.AppSettings[clave]
                               ?? valorPredeterminado;

            return configurados
                .Split(
                    new[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    int id;
                    return int.TryParse(x.Trim(), out id) ? id : -1;
                })
                .Contains(usuarioId);
        }

        private static DateTime PrimerDia(DateTime periodo)
        {
            return new DateTime(periodo.Year, periodo.Month, 1);
        }
    }
}
