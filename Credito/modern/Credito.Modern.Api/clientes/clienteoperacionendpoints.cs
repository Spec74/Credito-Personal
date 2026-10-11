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

namespace Credito.Modern.Api.Clientes;

internal static class ClienteOperacionEndpoints
{
    public static void MapClienteOperacionEndpoints(this WebApplication app)
    {

        app.MapGet(
                "/api/v1/clientes/existe-documento",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    string? documento,
                    int? excluirPersonaId,
                    IClienteDetalleReadService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ClientesExisteDocumento");
                    try
                    {
                        var existe = await clientes
                            .ExisteDocumentoAsync(documento ?? string.Empty, excluirPersonaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(existe);
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
                        log.LogError(ex, "Error al verificar documento de cliente");
                        var detail = "No se pudo verificar el documento.";
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
            .WithName("ClientesExisteDocumento")
            .WithSummary("Paridad ClienteController.ValidarClienteDNI (ya existe como cliente).")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/clientes/{personaId:int}",
                async Task<Results<Ok<ClienteDetalleDto>, ProblemHttpResult>> (
                    int personaId,
                    IClienteDetalleReadService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("ClientesDetalle");
                    try
                    {
                        var detalle = await clientes
                            .ObtenerPorPersonaIdAsync(personaId, ct)
                            .ConfigureAwait(false);
                        if (detalle is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe un cliente con el personaId indicado.");
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
                        log.LogError(ex, "Error al obtener detalle de cliente");
                        var detail = "No se pudo obtener el cliente.";
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
            .WithName("ClientesDetalle")
            .WithSummary("Paridad ClienteController.ObtenerClientePersona (formulario GuardarCliente).")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ClienteDetalleDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/clientes/guardar",
                async Task<Results<Ok<GuardarClienteResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarClienteRequest body,
                    IClienteWriteService clientes,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("ClientesGuardar");
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

                        var result = await clientes
                            .GuardarAsync(body, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Solicitud inválida al guardar cliente");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "La solicitud enviada no es válida." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al guardar cliente");
                        var detail = "No se pudo guardar el cliente.";
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
            .WithName("ClientesGuardar")
            .WithSummary("Paridad ClienteController.GuardarCliente. usuarioRegId = JWT.")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<GuardarClienteResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/clientes/{personaId:int}/ubicacion",
                async Task<Results<Ok, ProblemHttpResult>> (
                    int personaId,
                    ActualizarUbicacionClienteRequest body,
                    IClienteWriteService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("ClientesUbicacion");
                    try
                    {
                        await clientes
                            .ActualizarUbicacionAsync(personaId, body, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok();
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Ubicación inválida personaId={PersonaId}", personaId);
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "La ubicación enviada no es válida."
                                : ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Misma distinción que guardar/detalle: config → 503; sin fila Cliente → 404.
                        if (ex.Message.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("CreditoDatabase", StringComparison.OrdinalIgnoreCase))
                        {
                            log.LogWarning(ex, "Cadena de conexión no configurada");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta");
                        }

                        log.LogWarning(ex, "No se pudo actualizar ubicación personaId={PersonaId}", personaId);
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Cliente no encontrado",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se encontró el cliente."
                                : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error SQL al actualizar ubicación personaId={PersonaId}", personaId);
                        var detail = "No se pudo actualizar la ubicación del cliente.";
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
            .WithName("ClientesActualizarUbicacion")
            .WithSummary("Registra latitud/longitud GPS del cliente (captura en campo o ficha).")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/clientes/{personaId:int}/activar",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    int personaId,
                    IClienteWriteService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("ClientesActivar");
                    try
                    {
                        var estado = await clientes.ToggleActivoAsync(personaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(estado);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al activar/desactivar cliente");
                        var detail = "No se pudo cambiar el estado del cliente.";
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
            .WithName("ClientesActivar")
            .WithSummary("Paridad ClienteController.Activar (toggle Estado).")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/clientes/{personaId:int}/bloquear",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    int personaId,
                    IClienteWriteService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("ClientesBloquear");
                    try
                    {
                        var bloqueado = await clientes.ToggleBloqueadoAsync(personaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(bloqueado);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al bloquear/desbloquear cliente");
                        var detail = "No se pudo cambiar el bloqueo del cliente.";
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
            .WithName("ClientesBloquear")
            .WithSummary("Paridad ClienteController.Bloquear (toggle Bloqueado).")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/clientes/buscar",
                async Task<Results<Ok<List<ClienteBuscarItemDto>>, ProblemHttpResult>> (
                    string? term,
                    IClienteBuscarReadService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ClientesBuscar");
                    try
                    {
                        var items = await clientes.BuscarAsync(term ?? string.Empty, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al buscar clientes");
                        var detail = "No se pudo buscar clientes.";
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
            .WithName("ClientesBuscar")
            .WithSummary("Autocomplete clientes activos por DNI, nombre, código o celular (extiende ClienteBL.BuscarCliente). Mínimo 2 caracteres.")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ClienteBuscarItemDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/usuarios/buscar",
                async Task<Results<Ok<List<ClienteBuscarItemDto>>, ProblemHttpResult>> (
                    string? term,
                    IUsuarioBuscarReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("UsuariosBuscar");
                    try
                    {
                        var items = await usuarios.BuscarAsync(term ?? string.Empty, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al buscar usuarios");
                        var detail = "No se pudo buscar usuarios.";
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
            .WithName("UsuariosBuscar")
            .WithSummary("Autocomplete usuarios activos (paridad ClienteController.BuscarUsuario). Mínimo 2 caracteres.")
            .WithTags("usuarios")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ClienteBuscarItemDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }
}
