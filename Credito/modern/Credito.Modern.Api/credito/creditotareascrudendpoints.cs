using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTareas;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.Dashboard;
using Credito.Modern.Application.Documentos;
using Credito.Modern.Application.Integraciones;
using Credito.Modern.Application.Inventario;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Ventas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

internal static class CreditoTareasCrudEndpoints
{
    public static void MapCreditoTareasCrudEndpoints(this WebApplication app)
    {

        app.MapGet(
                "/api/v1/credito/tareas",
                async Task<Results<Ok<List<TareaListItemDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? estado,
                    ITareasReadService tareas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId) || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var log = loggerFactory.CreateLogger("CreditoTareas");
                    try
                    {
                        var items = await tareas
                            .ListarPorUsuarioAsync(usuarioId, oficinaId, estado, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar tareas");
                        var detail = "No se pudo listar las tareas.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasListar")
            .WithSummary("Listado de tareas del gestor (paridad TareaBL.ListarTareasUsuario). estado=PEN|COM u omitir.")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<TareaListItemDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/tareas/puede-editar",
                async Task<Results<Ok<PuedeEditarTareaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ITareasReadService tareas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1
                        || !MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId) || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Token sin usuario u oficina válidos.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoTareasPermisos");
                    try
                    {
                        var puede = await tareas.PuedeEditarTareaAsync(usuarioId, oficinaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new PuedeEditarTareaResponse(puede));
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al consultar permisos de tareas");
                        var detail = "No se pudo consultar permisos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasPuedeEditar")
            .WithSummary("Paridad TareaBL.PuedeEditarTarea (Administrador o Aprobador).")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<PuedeEditarTareaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/tareas/creditos-buscar",
                async Task<Results<Ok<List<CreditoTareaBuscarDto>>, ProblemHttpResult>> (
                    string? term,
                    ITareasReadService tareas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("CreditoTareasBuscar");
                    try
                    {
                        var items = await tareas.BuscarCreditosAsync(term ?? string.Empty, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al buscar créditos para tareas");
                        var detail = "No se pudo buscar créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasCreditosBuscar")
            .WithSummary("Paridad TareasController.BuscarCreditosDesembolsados.")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CreditoTareaBuscarDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/tareas/{tareaId:int}",
                async Task<Results<Ok<TareaDetalleDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int tareaId,
                    ITareasReadService tareas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1
                        || !MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId) || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Token sin usuario u oficina válidos.");
                    }

                    if (tareaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tareaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoTareasDetalle");
                    try
                    {
                        var detalle = await tareas.ObtenerDetalleAsync(tareaId, usuarioId, oficinaId, ct).ConfigureAwait(false);
                        if (detalle is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Tarea no encontrada o sin acceso.");
                        }

                        return TypedResults.Ok(detalle);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al obtener tarea");
                        var detail = "No se pudo obtener la tarea.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasObtener")
            .WithSummary("Paridad TareasController.ObtenerTarea.")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TareaDetalleDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/tareas/guardar",
                async Task<Results<Ok<GuardarTareaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarTareaRequest body,
                    ITareasWriteService tareasWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1
                        || !MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId) || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Token sin usuario u oficina válidos.");
                    }

                    if (body.CreditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Debe seleccionar un crédito.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoTareasGuardar");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await tareasWrite
                            .GuardarAsync(body, usuarioId, oficinaId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("permiso", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("acceso", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al guardar tarea");
                        var detail = "No se pudo guardar la tarea.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasGuardar")
            .WithSummary("Paridad TareasController.GuardarTarea.")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<GuardarTareaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/tareas/{tareaId:int}/eliminar",
                async Task<Results<Ok<TareaOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int tareaId,
                    ITareasWriteService tareasWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1
                        || !MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId) || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Token sin usuario u oficina válidos.");
                    }

                    if (tareaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tareaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoTareasEliminar");
                    try
                    {
                        var response = await tareasWrite.EliminarAsync(tareaId, usuarioId, oficinaId, ct).ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("permiso", StringComparison.OrdinalIgnoreCase)
                                                               || ex.Message.Contains("acceso", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al eliminar tarea");
                        var detail = "No se pudo eliminar la tarea.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasEliminar")
            .WithSummary("Paridad TareasController.EliminarTarea.")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TareaOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/tareas/{tareaId:int}/completar",
                async Task<Results<Ok<TareaOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int tareaId,
                    CompletarTareaRequest body,
                    ITareasWriteService tareasWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1
                        || !MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId) || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Token sin usuario u oficina válidos.");
                    }

                    if (tareaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "tareaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CreditoTareasCompletar");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await tareasWrite
                            .CompletarAsync(tareaId, body.Completada, usuarioId, oficinaId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: response.Mensaje);
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("acceso", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al completar tarea");
                        var detail = "No se pudo actualizar la tarea.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTareasCompletar")
            .WithSummary("Paridad TareasController.CompletarTarea.")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TareaOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }
}
