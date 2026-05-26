using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ITB.VENDIX.BL;
using ITB.VENDIX.DA;
using Microsoft.Reporting.WebForms;

namespace Web.Controllers.Credito
{
    public class TareasController : Controller
    {
        // GET: Tareas
        public ActionResult Index()
        {
            ViewBag.PuedeEditarTarea = TareaBL.PuedeEditarTarea();
            return View();
        }

        // Buscar créditos desembolsados para autocomplete
        public JsonResult BuscarCreditosDesembolsados(string term)
        {
            using (var db = new VENDIXEntities())
            {
                var estadosValidos = new[] { "CRE", "PEN", "APR", "DES" };
                
                var creditos = db.Credito
                    .Where(c => estadosValidos.Contains(c.Estado) && 
                           (c.Persona.NumeroDocumento.Contains(term) || 
                            c.Persona.NombreCompleto.Contains(term)))
                    .OrderByDescending(c => c.CreditoId)
                    .Take(15)
                    .Select(c => new
                    {
                        id = c.CreditoId,
                        personaId = c.PersonaId,
                        dni = c.Persona.NumeroDocumento,
                        nombre = c.Persona.NombreCompleto,
                        monto = c.MontoCredito,
                        label = c.Persona.NumeroDocumento + " - " + c.Persona.NombreCompleto + " (Crédito #" + c.CreditoId + " - S/. " + c.MontoCredito + ")",
                        value = c.Persona.NumeroDocumento + " - " + c.Persona.NombreCompleto + " (Crédito #" + c.CreditoId + " - S/. " + c.MontoCredito + ")"
                    })
                    .ToList();

                return Json(creditos, JsonRequestBehavior.AllowGet);
            }
        }

        // Listar tareas del usuario
        public JsonResult ListarTareas(string estado = "PEN")
        {
            var tareas = TareaBL.ListarTareasUsuario(estado);
            return Json(tareas, JsonRequestBehavior.AllowGet);
        }

        // Obtener detalle de una tarea
        public JsonResult ObtenerTarea(int id)
        {
            var tarea = TareaBL.ObtenerTareaDetalle(id);
            return Json(tarea, JsonRequestBehavior.AllowGet);
        }

        // Guardar tarea (nueva o editar)
        [HttpPost]
        public JsonResult GuardarTarea(TareaGuardarViewModel model)
        {
            try
            {
                if (model.CreditoId == 0)
                {
                    return Json(new { success = false, message = "Debe seleccionar un crédito" });
                }

                var tareaId = TareaBL.GuardarTarea(model);
                
                if (tareaId > 0)
                {
                    return Json(new { success = true, tareaId = tareaId, message = "Tarea guardada correctamente" });
                }
                
                return Json(new { success = false, message = "Error al guardar la tarea" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Eliminar tarea
        [HttpPost]
        public JsonResult EliminarTarea(int id)
        {
            try
            {
                var resultado = TareaBL.EliminarTarea(id);
                
                if (resultado)
                {
                    return Json(new { success = true, message = "Tarea eliminada correctamente" });
                }
                
                return Json(new { success = false, message = "Error al eliminar la tarea" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Completar tarea
        [HttpPost]
        public JsonResult CompletarTarea(int id, bool completada)
        {
            try
            {
                var resultado = TareaBL.CompletarTarea(id, completada);
                
                if (resultado)
                {
                    return Json(new { success = true, message = completada ? "Tarea completada" : "Tarea marcada como pendiente" });
                }
                
                return Json(new { success = false, message = "Error al actualizar la tarea" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        
    }

    // Clase para los datos del reporte
    public class TareaReporteData
    {
        public int Nro { get; set; }
        public string Cliente { get; set; }
        public string Analista { get; set; }
        public string Subtareas { get; set; }
        public string DetalleSubtareas { get; set; }
        public string Estado { get; set; }
    }
}