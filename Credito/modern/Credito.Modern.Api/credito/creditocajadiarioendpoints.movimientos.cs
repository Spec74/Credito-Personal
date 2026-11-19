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

/// <summary>Caja diario — cierre, reconciliar, anular.</summary>
internal static partial class CreditoCajaDiarioEndpoints
{
    private static void MapCajaDiarioCierreYMovimientos(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/validar-cierre-caja-diario",
                async Task<Results<Ok<ValidarCierreCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? cajaDiarioId,
                    ICajaDiarioCierreService cajaDiarioCierre,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId y cajaDiarioId son obligatorios y deben ser enteros >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId.Value);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(oficinaId.Value, cajaDiarioId.Value, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreCajaDiario");
                    try
                    {
                        var response = await cajaDiarioCierre
                            .ValidarCierreAsync(cajaDiarioId.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        log.LogWarning(ex, "Caja diario no encontrada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Configuraci?n incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre de caja diario");
                        var detail = "No se pudo validar el cierre de la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreCajaDiario")
            .WithSummary(
                "Solo lectura: validaciones previas a cerrar caja (paridad CreditoController.ValidarCierreCajaDiario / CajaDiarioBL). oficinaId = vendix:oficina_id. CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-caja-diario",
                async Task<Results<Ok<CerrarCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CerrarCajaDiarioRequest body,
                    ICajaDiarioCierreService cajaDiarioCierre,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
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
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("CerrarCajaDiario");
                    try
                    {
                        var validacion = await cajaDiarioCierre
                            .ValidarCierreAsync(body.CajaDiarioId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeCerrar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No se puede cerrar la caja",
                                detail: string.Join("; ", validacion.Blockers),
                                extensions: new Dictionary<string, object?> { ["blockers"] = validacion.Blockers });
                        }

                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await cajaDiarioCierre
                            .CerrarAsync(body.CajaDiarioId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        log.LogWarning(ex, "Caja diario no encontrada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cierre de caja rechazado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "No se puede cerrar la caja",
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.");
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
                        log.LogError(ex, "Error al cerrar caja diario");
                        var detail = "No se pudo cerrar la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarCajaDiario")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.CerrarCajaDiario (UPDATE CajaDiario/Caja + CREDITO.ActualizarClientesNuevos). Rechaza 409 si validar-cierre falla. usuarioModId = vendix:usuario_id; fecha = usp_FechaBD. Distinto de cerrar-cajas-diarios (transferencia b?veda).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CerrarCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/reconciliar-caja-diario",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ReconciliarCajaDiarioRequest body,
                    ICajaDiarioOperacionWriteService cajaDiarioOperacion,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ReconciliarCajaDiario");
                    try
                    {
                        var response = await cajaDiarioOperacion
                            .ReconciliarCajaDiarioAsync(body.CajaDiarioId, ct)
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ReconciliarCajaDiario");
                        var detail = "No se pudo reconciliar la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoReconciliarCajaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_ReconciliarCajaDiario(CajaDiarioId). Paridad CajaDiarioBL.ConciliarCajaDiario / CreditoController.ConciliarCajaDiario.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/recalcular-caja-diario",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RecalcularCajaDiarioRequest body,
                    ICajaDiarioOperacionWriteService cajaDiarioOperacion,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("RecalcularCajaDiario");
                    try
                    {
                        var response = await cajaDiarioOperacion
                            .RecalcularCajaDiarioAsync(body.CajaDiarioId, ct)
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RecalcularCajaDiario");
                        var detail = "No se pudo recalcular la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRecalcularCajaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_RecalcularCajaDiario(CajaDiarioId). Mantenimiento de saldos de caja diario (edmx / inventario Fase 0).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-saldos-caja-diario",
                async Task<Results<Ok<object>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirSaldosCajaDiarioRequest body,
                    ICajaDiarioTransferWriteService transferWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var edgeError = ProblemResults.IfInvalid(
                        TesoreriaValidacion.ValidarMovimiento(body.Importe, body.Descripcion, "descripcion"));
                    if (edgeError is not null)
                        return edgeError;

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
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("TransferirSaldosCajaDiario");
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

                        var tipoPagoId = body.TipoPagoId is > 0 ? body.TipoPagoId.Value : (short)1;
                        var (error, success) = await transferWrite
                            .TransferirSaldosAsync(
                                body.OficinaId,
                                body.CajaDiarioId,
                                usuarioId,
                                body.Importe,
                                body.Descripcion,
                                body.CajaIdDestino,
                                serverTime.Value,
                                tipoPagoId,
                                ct)
                            .ConfigureAwait(false);

                        if (!success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error ?? "Error al grabar.");
                        }

                        return TypedResults.Ok((object)new { success = true });
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al transferir saldos caja diario");
                        var detail = "No se pudo transferir saldos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirSaldosCajaDiario")
            .WithSummary(
                "Escritura: transferencia caja→caja o caja→bóveda preservando tipoPagoId (Yape René, Plin, BCP…).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/confirmar-clave-caja-diario",
                async Task<Results<Ok<ConfirmarClaveCajaDiarioResponse>, ProblemHttpResult>> (
                    ConfirmarClaveCajaDiarioRequest body,
                    IConfirmarClaveCajaDiarioService confirmarClave,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ConfirmarClaveCajaDiario");
                    try
                    {
                        var response = await confirmarClave
                            .VerificarAsync(body.Clave ?? string.Empty, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al confirmar clave caja diario");
                        var detail = "No se pudo validar la clave.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoConfirmarClaveCajaDiario")
            .WithSummary("Paridad CajaDiarioController.ConfirmarClave (usuario ADMVENDIX).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ConfirmarClaveCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-anular-movimiento-caja",
                async Task<Results<Ok<ValidarAnularMovimientoCajaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int movimientoCajaId,
                    IMovimientoCajaScopeReadService movimientoScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(oficinaId, movimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarAnularMovimientoCaja");
                    try
                    {
                        var (scopeError, _) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(oficinaId, movimientoCajaId, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }

                        var bloqueado = await movimientoScope
                            .EsBloqueoAnularPorPagosCuotaAsync(movimientoCajaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new ValidarAnularMovimientoCajaResponse(bloqueado));
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
                        log.LogError(ex, "Error al validar anulaci?n de movimiento de caja");
                        var detail = "No se pudo validar la anulaci?n del movimiento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarAnularMovimientoCaja")
            .WithSummary(
                "Lectura previa: paridad CreditoController.ValidarAnularMovimientoCaja + CajaDiario (INI con cuotas PAG ? bloqueado).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarAnularMovimientoCajaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/movimiento-caja-anular",
                async Task<Results<Ok<MovimientoCajaAnularPreviewDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int movimientoCajaId,
                    IMovimientoCajaScopeReadService movimientoScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(oficinaId, movimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("MovimientoCajaAnular");
                    try
                    {
                        var preview = await movimientoScope
                            .GetAnularPreviewAsync(movimientoCajaId, ct)
                            .ConfigureAwait(false);
                        if (preview is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Movimiento no encontrado.");
                        }

                        if (preview.OficinaId != oficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "El movimiento de caja no pertenece a la oficina del token JWT.");
                        }

                        return TypedResults.Ok(preview);
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
                        log.LogError(ex, "Error al consultar movimiento de caja para anular");
                        var detail = "No se pudo consultar el movimiento de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientoCajaAnular")
            .WithSummary(
                "Lectura: paridad CajaDiarioController.ObtenerMovimientoCajaAnular (detalle antes de anular en Saldos).")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoCajaAnularPreviewDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/movimiento-caja-detalle-ov",
                async Task<Results<Ok<MovimientoCajaDetalleOvDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int movimientoCajaId,
                    IMovimientoCajaDetalleOvReadService detalleOv,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(oficinaId, movimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("MovimientoCajaDetalleOv");
                    try
                    {
                        var dto = await detalleOv.GetAsync(movimientoCajaId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Movimiento no encontrado.");
                        }

                        if (dto.OficinaId != oficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "El movimiento de caja no pertenece a la oficina del token JWT.");
                        }

                        return TypedResults.Ok(dto);
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
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al consultar detalle OV del movimiento");
                        var detail = "No se pudo consultar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientoCajaDetalleOv")
            .WithSummary(
                "Lectura: paridad CajaDiarioBL.MostrarDetalleOvMovCaja (INI/CON/CUO ? l?neas OrdenVentaDet).")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoCajaDetalleOvDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/anular-movimiento-caja",
                async Task<Results<Ok<AnularMovimientoCajaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AnularMovimientoCajaRequest body,
                    IMovimientoCajaScopeReadService movimientoScope,
                    IMovimientoCajaWriteService movimientoCajaWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(body.OficinaId, body.MovimientoCajaId);
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

                    var log = loggerFactory.CreateLogger("AnularMovimientoCaja");
                    try
                    {
                        var (scopeError, scope) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(body.OficinaId, body.MovimientoCajaId, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }

                        var anulableError = CajaCreditoWriteGuards.ValidateMovimientoAnulable(scope!);
                        if (anulableError is not null)
                        {
                            return anulableError;
                        }

                        var bloqueadoPorPagos = await movimientoScope
                            .EsBloqueoAnularPorPagosCuotaAsync(body.MovimientoCajaId, ct)
                            .ConfigureAwait(false);
                        if (bloqueadoPorPagos)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Conflicto",
                                detail: "Tiene Pagos de cuotas, No se Puede Anular el Cr?dito");
                        }

                        var observacion = body.Observacion?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(observacion))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Par?metros inv?lidos",
                                detail: "Ingrese la observaci?n para anular.");
                        }

                        var response = await movimientoCajaWrite
                            .AnularAsync(body.MovimientoCajaId, observacion, usuarioId, ct)
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_MovimientoCaja_Del");
                        var detail = "No se pudo anular el movimiento de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAnularMovimientoCaja")
            .WithSummary(
                "Escritura: CREDITO.usp_MovimientoCaja_Del. Paridad CajaDiarioBL.AnularMovimientoCaja / CreditoController.AnularMovimientoCaja.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAnularMovimientoCaja)
            .Produces<AnularMovimientoCajaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



    }
}