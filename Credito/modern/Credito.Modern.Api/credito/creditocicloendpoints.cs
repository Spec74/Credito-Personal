using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Validation;
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
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.Ventas;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

/// <summary>Ciclo de vida: aprobar, anular, reprogramar, prorrogar, crear y rechazar.</summary>
internal static class CreditoCicloEndpoints
{
    public static void MapCreditoCicloEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/validar-anular-credito",
                async Task<Results<Ok<ValidarAnularCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int creditoId,
                    ICreditoAnulacionReadService creditoAnulacion,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(oficinaId, creditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(oficinaId, creditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarAnularCredito");
                    try
                    {
                        var response = await creditoAnulacion
                            .ValidarAnularAsync(creditoId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar anulaci?n de cr?dito");
                        var detail = "No se pudo validar la anulaci?n del cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarAnularCredito")
            .WithSummary("Lectura previa: paridad CreditoController.ValidarAnularCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarAnularCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/creditos-por-aprobar",
                async Task<Results<Ok<CreditosPorAprobarListResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    string? buscar,
                    int? page,
                    int? pageSize,
                    string? sortField,
                    string? sortOrder,
                    ICreditosPorAprobarReadService creditosPorAprobar,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var oficina = oficinaId.GetValueOrDefault();
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficina);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("CreditosPorAprobar");
                    try
                    {
                        var response = await creditosPorAprobar
                            .ListarAsync(
                                oficina,
                                buscar,
                                page is null or < 1 ? 1 : page.Value,
                                pageSize is null or < 1 ? 15 : pageSize.Value,
                                string.IsNullOrWhiteSpace(sortField) ? "Agente" : sortField,
                                string.IsNullOrWhiteSpace(sortOrder) ? "asc" : sortOrder,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar cr?ditos por aprobar");
                        var detail = "No se pudo obtener el listado de cr?ditos pendientes de aprobaci?n.";
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
            .WithName("CreditoCreditosPorAprobar")
            .WithSummary(
                "Solo lectura: cr?ditos Estado=PEN (paridad CreditoBL.LstCreditoAprobarJGrid / CreditoAprobarController).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditosPorAprobarListResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/aprobar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AprobarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.Opcion is not (0 or 1))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "opcion debe ser 0 (primera aprobaci?n) o 1 (segunda aprobaci?n).");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("AprobarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .AprobarAsync(body.CreditoId, body.Opcion, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_Credito_Upd");
                        var detail = "No se pudo aprobar el cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAprobarCredito")
            .WithSummary(
                "Escritura: CREDITO.usp_Credito_Upd. opcion 0 = AprobarCredito1ra; 1 = AprobarCredito (paridad CreditoBL.AprobarCredito).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/anular-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AnularCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoAnulacionReadService creditoAnulacion,
                    ICreditoOficinaReadService creditoOficina,
                    IConfirmarClaveCajaDiarioService confirmarClave,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (string.IsNullOrWhiteSpace(body.Observacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "observacion es obligatoria.");
                    }

                    if (string.IsNullOrWhiteSpace(body.ClaveAutorizacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "claveAutorizacion es obligatoria.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("AnularCredito");
                    try
                    {
                        var autorizacion = await confirmarClave
                            .VerificarAsync(body.ClaveAutorizacion, ct)
                            .ConfigureAwait(false);
                        if (!autorizacion.Autorizado)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "No autorizado",
                                detail: autorizacion.Mensaje ?? "Clave de autorizaci?n no v?lida.");
                        }

                        var validacion = await creditoAnulacion
                            .ValidarAnularAsync(body.CreditoId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeAnular)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Anulaci?n no permitida",
                                detail: "El cr?dito no cumple las condiciones para anular (paridad ValidarAnularCredito).");
                        }

                        var response = await creditoCiclo
                            .AnularAsync(body.CreditoId, body.Observacion, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_Credito_Del");
                        var detail = "No se pudo anular el cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAnularCredito")
            .WithSummary("Escritura: CREDITO.usp_Credito_Del. Requiere validar-anular-credito OK (409 si no).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/reprogramar-credito",
                async Task<Results<Ok<ReprogramarCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ReprogramarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ReprogramarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .ReprogramarAsync(body.CreditoId, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ReprogramarCredito");
                        var detail = "No se pudo reprogramar el cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoReprogramarCredito")
            .WithSummary("Escritura: CREDITO.usp_ReprogramarCredito. Paridad CreditoBL.ReprogramarCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolSoloAdministrador)
            .Produces<ReprogramarCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/prorrogar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ProrrogarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.Dias < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "dias debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCreditoOficinaAsync(body.OficinaId, body.CreditoId, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ProrrogarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .ProrrogarAsync(body.CreditoId, body.Dias, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ProrrogarCredito");
                        var detail = "No se pudo prorrogar el cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoProrrogarCredito")
            .WithSummary("Escritura: CREDITO.usp_ProrrogarCredito. Paridad CreditoBL.ProrrogarCredito / CreditoController.ProrrogarCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/crear-solicitud-credito",
                async Task<Results<Ok<CrearSolicitudCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearSolicitudCreditoRequest body,
                    ICreditoSolicitudWriteService creditoSolicitud,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CrearSolicitudCredito");
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

                        var response = await creditoSolicitud
                            .CrearSolicitudAsync(body.OficinaId, body.PersonaId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear solicitud de cr?dito");
                        var detail = "No se pudo crear la solicitud de cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCrearSolicitudCredito")
            .WithSummary("Escritura EF: paridad CreditoBL.CrearSolicitudCredito (estado CRE). Devuelve solicitudCreditoId.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CrearSolicitudCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/crear-credito",
                async Task<Results<Ok<CrearCreditoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoScopeReadService creditoScope,
                    IProductoReadService productos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.SolicitudCreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.ProductoId < 1 || body.NumeroCuotas < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "productoId y numeroCuotas deben ser >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(body.TipoCuota) || string.IsNullOrWhiteSpace(body.Modalidad)
                        || string.IsNullOrWhiteSpace(body.IndGastosAdm))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "tipoCuota, modalidad e indGastosAdm son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scope = await creditoScope.GetScopeAsync(body.SolicitudCreditoId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la solicitud de cr?dito indicada.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La solicitud no pertenece a la oficina del token JWT.");
                    }

                    if (!string.Equals(scope.Estado, "CRE", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: $"La solicitud no est? en estado CRE (actual: {scope.Estado}).");
                    }

                    var log = loggerFactory.CreateLogger("CrearCredito");
                    try
                    {
                        var producto = await productos.GetActivoByIdAsync(body.ProductoId, ct).ConfigureAwait(false);
                        if (producto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inv?lida",
                                detail: "El producto de cr?dito no existe o no est? activo.");
                        }

                        if (body.InteresMensual < producto.InteresMinima
                            || body.InteresMensual > producto.InteresMaxima)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Solicitud inv?lida",
                                detail: $"El inter?s debe estar entre {producto.InteresMinima:N2}% y {producto.InteresMaxima:N2}%.");
                        }

                        var moneyError = ProblemResults.IfInvalid(
                            ValidationGate.First(
                                MoneyRules.RequirePositive(body.MontoCredito, "montoCredito"),
                                MoneyRules.RequireNonNegative(body.MontoInicial, "montoInicial"),
                                MoneyRules.RequireNonNegative(body.MontoGastosAdm, "montoGastosAdm")));
                        if (moneyError is not null)
                        {
                            return moneyError;
                        }

                        var response = await creditoCiclo
                            .CrearDesdeSolicitudAsync(
                                body.SolicitudCreditoId,
                                body.ProductoId,
                                body.TipoCuota,
                                body.MontoInicial,
                                body.MontoCredito,
                                body.MontoGastosAdm,
                                body.IndGastosAdm,
                                body.Modalidad,
                                body.NumeroCuotas,
                                body.InteresMensual,
                                body.FechaPrimerPago,
                                body.Observacion ?? string.Empty,
                                usuarioId,
                                body.IndCentralRiesgo,
                                body.Prenda,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Solicitud inválida al generar crédito");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Datos inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "La solicitud enviada no es válida."
                                : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_Credito_Ins");
                        var detail = "No se pudo generar el cr?dito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCrearCredito")
            .WithSummary(
                "Escritura: CREDITO.usp_Credito_Ins. Paridad GenerarCredito; solicitudCreditoId en body (no sesi?n MVC).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CrearCreditoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/rechazar-credito",
                async Task<Results<Ok<CreditoCicloOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RechazarCreditoRequest body,
                    ICreditoCicloWriteService creditoCiclo,
                    ICreditoScopeReadService creditoScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCreditoIds(body.OficinaId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scope = await creditoScope.GetScopeAsync(body.CreditoId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el cr?dito indicado.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El cr?dito no pertenece a la oficina del token JWT.");
                    }

                    if (!CreditoRechazoReglas.EsPendiente(scope.Estado))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "No se puede rechazar",
                            detail: CreditoRechazoReglas.MensajeSiNoRechazable(scope.Estado, false));
                    }

                    var log = loggerFactory.CreateLogger("RechazarCredito");
                    try
                    {
                        var response = await creditoCiclo
                            .RechazarAsync(body.CreditoId, scope.OrdenVentaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message.Contains("Configure CreditoDatabase", StringComparison.Ordinal))
                        {
                            log.LogWarning(ex, "Cadena de conexi?n no configurada");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuraci?n incompleta");
                        }

                        log.LogWarning(ex, "Rechazo no aplicable");
                        return TypedResults.Problem(
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se pudo rechazar el crédito."
                                : ex.Message,
                            statusCode: StatusCodes.Status409Conflict,
                            title: "No se puede rechazar");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al rechazar cr?dito");
                        var detail = CreditoRechazoReglas.MensajeDesdeDb(ex);
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status409Conflict,
                            title: "No se pudo rechazar");
                    }
                })
            .WithName("CreditoRechazarCredito")
            .WithSummary(
                "Escritura: usp_OrdenVenta_Del o usp_SolicitudCredito_Del. Limpia hijos PEN (Aprobacion/Prenda/...). Paridad CreditoBL.RechazarCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAprobador1OAdministrador)
            .Produces<CreditoCicloOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
