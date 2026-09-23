using System;
using System.Collections.Generic;
using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class TareaBL : Repositorio<Tarea>
    {
        /// <summary>
        /// Obtiene las tareas del usuario actual con sus subtareas mapeadas
        /// </summary>
        /// <param name="estado">PEN=Pendientes, COM=Completadas, null=Todas</param>
        public static List<TareaViewModel> ListarTareasUsuario(string estado = "PEN")
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var esAdmin = PuedeEditarTarea();

            using (var db = new VENDIXEntities())
            {
                var query = db.Tarea.AsQueryable();
                if (!esAdmin)
                {
                    query = query.Where(t => t.Credito.UsuarioRegId == usuarioId);
                }

                // Aplicar filtro de estado
                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(t => t.Estado == estado);
                }

                var tareas = query
                    .OrderByDescending(t => t.FechaCreacion)
                    .Select(t => new TareaViewModel
                    {
                        TareaId = t.TareaId,
                        CreditoId = t.CreditoId,
                        ClienteDni = t.Credito.Persona.NumeroDocumento,
                        ClienteNombre = t.Credito.Persona.NombreCompleto,
                        MontoCredito = t.Credito.MontoCredito,
                        NombreUsuario = t.Credito.Usuario.NombreUsuario,
                        FechaCreacion = t.FechaCreacion,
                        FechaCompletada = t.FechaCompletada,
                        Estado = t.Estado,
                        TotalSubtareas = t.Subtarea.Count(),
                        SubtareasCompletadas = t.Subtarea.Count(s => s.Completada),
                        // SE AÑADIÓ: Subtareas para pintarlas en la grilla sin abrir modales
                        Subtareas = t.Subtarea.Select(s => new SubtareaViewModel
                        {
                            SubtareaId = s.SubtareaId,
                            TareaId = s.TareaId,
                            Titulo = s.Titulo,
                            Completada = s.Completada,
                            FechaCompletada = s.FechaCompletada
                        }).ToList()
                    })
                    .ToList();

                return tareas;
            }
        }

        /// <summary>
        /// Obtiene una tarea con sus subtareas
        /// </summary>
        public static TareaDetalleViewModel ObtenerTareaDetalle(int tareaId)
        {
            using (var db = new VENDIXEntities())
            {
                var tarea = db.Tarea
                    .Where(t => t.TareaId == tareaId)
                    .Select(t => new TareaDetalleViewModel
                    {
                        TareaId = t.TareaId,
                        CreditoId = t.CreditoId,
                        ClienteDni = t.Credito.Persona.NumeroDocumento,
                        ClienteNombre = t.Credito.Persona.NombreCompleto,
                        MontoCredito = t.Credito.MontoCredito,
                        FechaCreacion = t.FechaCreacion,
                        FechaCompletada = t.FechaCompletada,
                        Estado = t.Estado,
                        Subtareas = t.Subtarea.Select(s => new SubtareaViewModel
                        {
                            SubtareaId = s.SubtareaId,
                            TareaId = s.TareaId,
                            Titulo = s.Titulo,
                            Completada = s.Completada,
                            FechaCompletada = s.FechaCompletada
                        }).ToList()
                    })
                    .FirstOrDefault();

                return tarea;
            }
        }

        /// <summary>
        /// Guarda una tarea nueva o actualiza una existente
        /// </summary>
        public static int GuardarTarea(TareaGuardarViewModel model)
        {
            using (var db = new VENDIXEntities())
            {
                Tarea tarea;

                if (model.TareaId == 0)
                {
                    // Nueva tarea
                    var tareaExistente = db.Tarea.FirstOrDefault(t => t.CreditoId == model.CreditoId && t.Estado == "PEN");

                    if (tareaExistente != null)
                    {
                        tarea = tareaExistente;
                    }
                    else
                    {
                        tarea = new Tarea
                        {
                            CreditoId = model.CreditoId,
                            FechaCreacion = DateTime.Now,
                            Estado = "PEN", // Pendiente
                            UsuarioCreadorId = VendixGlobal.GetUsuarioId()
                        };
                        db.Tarea.Add(tarea);
                        db.SaveChanges();
                    }
                }
                else
                {
                    // Editar tarea existente
                    tarea = db.Tarea.Find(model.TareaId);
                    if (tarea == null)
                        return 0;

                    tarea.CreditoId = model.CreditoId;

                    // Eliminar subtareas existentes
                    var subtareasExistentes = db.Subtarea.Where(s => s.TareaId == model.TareaId).ToList();
                    db.Subtarea.RemoveRange(subtareasExistentes);
                }

                // Agregar subtareas
                if (model.Subtareas != null && model.Subtareas.Count > 0)
                {
                    foreach (var sub in model.Subtareas)
                    {
                        // Validación para no duplicar exactamente la misma subtarea si ya estaba registrada en el grupo
                        bool yaExisteSubtarea = db.Subtarea.Any(s => s.TareaId == tarea.TareaId &&
                                                                     s.Titulo.ToUpper() == sub.Titulo.ToUpper().Trim() &&
                                                                     !s.Completada);
                        if (model.TareaId == 0 && yaExisteSubtarea)
                        {
                            continue; // Salta la inserción si esa observación ya está activa para el cliente
                        }

                        var subtarea = new Subtarea
                        {
                            TareaId = tarea.TareaId,
                            Titulo = sub.Titulo,
                            Completada = sub.Completada,
                            FechaCompletada = sub.Completada ? DateTime.Now : (DateTime?)null,
                        };
                        db.Subtarea.Add(subtarea);
                    }
                }
                db.SaveChanges();
                var todasSubtareas = db.Subtarea.Where(s => s.TareaId == tarea.TareaId).ToList();
                // Actualizar estado de la tarea
                var todasCompletadas = todasSubtareas.Count > 0 && todasSubtareas.All(s => s.Completada);

                if (todasCompletadas)
                {
                    tarea.Estado = "COM"; // Completada
                    tarea.FechaCompletada = DateTime.Now;
                }
                else
                {
                    tarea.Estado = "PEN"; // Pendiente
                    tarea.FechaCompletada = null;
                }

                db.SaveChanges();
                return tarea.TareaId;
            }
        }

        /// <summary>
        /// Elimina una tarea y sus subtareas
        /// </summary>
        public static bool EliminarTarea(int tareaId)
        {
            using (var db = new VENDIXEntities())
            {
                var tarea = db.Tarea.Find(tareaId);
                if (tarea == null)
                    return false;

                // Eliminar subtareas
                var subtareas = db.Subtarea.Where(s => s.TareaId == tareaId).ToList();
                db.Subtarea.RemoveRange(subtareas);

                // Eliminar tarea
                db.Tarea.Remove(tarea);
                db.SaveChanges();
                return true;
            }
        }

        /// <summary>
        /// Marca una tarea completa de golpe junto con sus subtareas
        /// </summary>
        public static bool CompletarTarea(int tareaId, bool completada)
        {
            using (var db = new VENDIXEntities())
            {
                var tarea = db.Tarea.Find(tareaId);
                if (tarea == null)
                    return false;

                tarea.Estado = completada ? "COM" : "PEN";
                tarea.FechaCompletada = completada ? DateTime.Now : (DateTime?)null;

                // Marcar todas las subtareas
                var subtareas = db.Subtarea.Where(s => s.TareaId == tareaId).ToList();
                foreach (var sub in subtareas)
                {
                    sub.Completada = completada;
                    sub.FechaCompletada = completada ? DateTime.Now : (DateTime?)null;
                }

                db.SaveChanges();
                return true;
            }
        }

        /// <summary>
        /// Modifica una subtarea individual inline y determina si la tarea completa fue resuelta (Solo si es Admin)
        /// </summary>
        public static bool CompletarSubtarea(int subtareaId, bool completada, out bool tareaCompletada)
        {
            tareaCompletada = false;
            using (var db = new VENDIXEntities())
            {
                var subtarea = db.Subtarea.Find(subtareaId);
                if (subtarea == null) return false;

                // 1. Cualquier usuario (analista/gestor) puede cambiar el estado de la subtarea inline
                subtarea.Completada = completada;
                subtarea.FechaCompletada = completada ? DateTime.Now : (DateTime?)null;
                db.SaveChanges();

                // 2. REGLA DE NEGOCIO: Solo el Administrador/Aprobador puede gatillar el auto-completado del expediente padre
                var esAdmin = PuedeEditarTarea();
                var padreId = subtarea.TareaId;
                var tareaPadre = db.Tarea.Find(padreId);

                if (tareaPadre != null)
                {
                    if (esAdmin)
                    {
                        var listadoSubtareas = db.Subtarea.Where(s => s.TareaId == padreId).ToList();
                        bool cerrarPadre = listadoSubtareas.Count > 0 && listadoSubtareas.All(s => s.Completada);

                        if (cerrarPadre)
                        {
                            tareaPadre.Estado = "COM";
                            tareaPadre.FechaCompletada = DateTime.Now;
                            tareaCompletada = true; // Retornamos true para activar el fadeOut en el cliente
                        }
                        else
                        {
                            tareaPadre.Estado = "PEN";
                            tareaPadre.FechaCompletada = null;
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        // Si es un usuario regular, la tarea padre mantiene su estado actual (PEN)
                        tareaCompletada = (tareaPadre.Estado == "COM");
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Verifica si el usuario actual puede crear tareas (solo Administrador o Aprobador)
        /// </summary>
        public static bool PuedeEditarTarea()
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            using (var db = new VENDIXEntities())
            {
                var tienePermiso = db.UsuarioRol
                    .Any(ur => ur.UsuarioId == usuarioId &&
                               ur.Rol != null &&
                               (ur.Rol.Denominacion.ToUpper().Contains("ADMINISTRADOR") ||
                                ur.Rol.Denominacion.ToUpper().Contains("APROBADOR")));

                return tienePermiso;
            }
        }

        /// <summary>
        /// Lista las tareas con sus subtareas para el reporte PDF
        /// </summary>
        public static List<TareaReporteViewModel> ListarTareasParaReporte(string estado = "PEN")
        {
            var usuarioId = VendixGlobal.GetUsuarioId();
            var esAdmin = PuedeEditarTarea();

            using (var db = new VENDIXEntities())
            {
                var query = db.Tarea.AsQueryable();
                if (!esAdmin)
                {
                    query = query.Where(t => t.Credito.UsuarioRegId == usuarioId);
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(t => t.Estado == estado);
                }

                var tareas = query
                    .Select(t => new TareaReporteViewModel
                    {
                        TareaId = t.TareaId,
                        CreditoId = t.CreditoId,
                        ClienteDni = t.Credito.Persona.NumeroDocumento,
                        ClienteNombre = t.Credito.Persona.NombreCompleto,
                        MontoCredito = t.Credito.MontoCredito,
                        NombreUsuario = t.Credito.Usuario.NombreUsuario,
                        FechaCreacion = t.FechaCreacion,
                        FechaCompletada = t.FechaCompletada,
                        Estado = t.Estado,
                        TotalSubtareas = t.Subtarea.Count(),
                        SubtareasCompletadas = t.Subtarea.Count(s => s.Completada),
                        Subtareas = t.Subtarea.Select(s => new SubtareaViewModel
                        {
                            SubtareaId = s.SubtareaId,
                            TareaId = s.TareaId,
                            Titulo = s.Titulo,
                            Completada = s.Completada,
                            FechaCompletada = s.FechaCompletada
                        }).ToList()
                    })
                    .OrderBy(t => t.NombreUsuario)
                    .ToList();

                return tareas;
            }
        }
    }

    #region ViewModels

    public class TareaViewModel
    {
        public int TareaId { get; set; }
        public int CreditoId { get; set; }
        public string ClienteDni { get; set; }
        public string ClienteNombre { get; set; }
        public decimal MontoCredito { get; set; }
        public string NombreUsuario { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCompletada { get; set; }
        public string Estado { get; set; }
        public int TotalSubtareas { get; set; }
        public int SubtareasCompletadas { get; set; }

        // MODIFICADO: Agregada propiedad para acoplar la colección inline en el listado general
        public List<SubtareaViewModel> Subtareas { get; set; }

        public string ClienteCompleto => $"{ClienteDni} - {ClienteNombre} (Crédito #{CreditoId} - S/. {MontoCredito:N2})";
        public string SubtareasTexto => $"{SubtareasCompletadas}/{TotalSubtareas}";
        public bool EstaCompletada => Estado == "COM";
    }

    public class TareaDetalleViewModel : TareaViewModel
    {
        // Hereda automáticamente Subtareas desde TareaViewModel
    }

    public class TareaReporteViewModel
    {
        public int TareaId { get; set; }
        public int CreditoId { get; set; }
        public string ClienteDni { get; set; }
        public string ClienteNombre { get; set; }
        public decimal MontoCredito { get; set; }
        public string NombreUsuario { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCompletada { get; set; }
        public string Estado { get; set; }
        public int TotalSubtareas { get; set; }
        public int SubtareasCompletadas { get; set; }
        public List<SubtareaViewModel> Subtareas { get; set; }

        public string ClienteCompleto => $"{ClienteDni} - {ClienteNombre} (Crédito #{CreditoId} - S/. {MontoCredito:N2})";
        public string SubtareasTexto => $"{SubtareasCompletadas}/{TotalSubtareas}";
        public bool EstaCompletada => Estado == "COM";
    }

    public class SubtareaViewModel
    {
        public int SubtareaId { get; set; }
        public int TareaId { get; set; }
        public string Titulo { get; set; }
        public bool Completada { get; set; }
        public DateTime? FechaCompletada { get; set; }
    }

    public class TareaGuardarViewModel
    {
        public int TareaId { get; set; }
        public int CreditoId { get; set; }
        public List<SubtareaGuardarViewModel> Subtareas { get; set; }
    }

    public class SubtareaGuardarViewModel
    {
        public string Titulo { get; set; }
        public bool Completada { get; set; }
    }

    #endregion
}