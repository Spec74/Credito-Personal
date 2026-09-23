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

        // Completar tarea de forma global
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

        // NUEVO MÉTODO: Cambiar estado de una subtarea individual inline
        [HttpPost]
        public JsonResult CompletarSubtarea(int id, bool completada)
        {
            try
            {
                bool tareaCompletada = false;
                // Pasamos el parámetro de salida 'out' para capturar el estado del padre
                var resultado = TareaBL.CompletarSubtarea(id, completada, out tareaCompletada);

                if (resultado)
                {
                    return Json(new
                    {
                        success = true,
                        tareaCompletada = tareaCompletada,
                        message = completada ? "Subtarea corregida" : "Subtarea pendiente"
                    });
                }

                return Json(new { success = false, message = "No se pudo actualizar el estado de la subtarea" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        // GET: Tareas/ObtenerSubtareasCatalogo
        [HttpGet]
        public JsonResult ObtenerSubtareasCatalogo()
        {
            // Lista base fija del sistema
            var listaBase = new List<string>
            {
                "FALTA FIRMA Y HUELLA DEL CLIENTE",
                "DNI CADUCADO",
                "DETALLAR CROQUIS",
                "FALTA FOTOS DEL NEGOCIO",
                "FIRMA Y HUELLA NO COINCIDE",
                "CORREGIR HUELLA DEL TITULAR",
                "FALTA DNI DEL AVAL",
                "DNI BORROSO",
                "FALTA RECIBO DE LUZ DEL AVAL",
                "FALTA FOTO SELFIE CON EL CLIENTE",
                "ACTUALIZAR LETRA DE CAMBIO",
                "FALTA FIRMA Y HUELLA DEL AVAL",
                "FALTA BOLETAS DEL NEGOCIO",
                "CORREGIR FORMATO DE RENOVACION (SIN BORRONES)",
                "HACER FIRMAR AVAL EN LETRA ACEPTANTE",
                "ACTUALIZAR NUMERO DE CELULAR EN SISTEMA Y FORMATO",
                "LLENAR CORRECTAMENTE LOS DATOS DEL CLIENTE EN FORMATO Y SISTEMA"
            };

            using (var db = new VENDIXEntities())
            {
                // 1. Jalamos los títulos históricos de la BD
                var personalizadas = db.Subtarea
                    .Where(s => s.Titulo != null && s.Titulo != "")
                    .Select(s => s.Titulo)
                    .ToList() // Pasamos a memoria en Linq para aplicar funciones de texto avanzadas
                    .Select(t => t.Trim().TrimEnd(',', ' ').ToUpper()) // BLINDAJE: Quita comas finales, espacios y fuerza Mayúsculas
                    .Distinct()
                    .ToList();

                // 2. Combinamos con la lista base garantizando que no existan duplicados de ningún tipo
                var catalogoCompleto = listaBase
                    .Union(personalizadas, StringComparer.OrdinalIgnoreCase)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .OrderBy(t => t)
                    .ToList();

                return Json(catalogoCompleto, JsonRequestBehavior.AllowGet);
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