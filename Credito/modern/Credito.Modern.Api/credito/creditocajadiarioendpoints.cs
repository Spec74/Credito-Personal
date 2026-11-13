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

/// <summary>Sesión, cierre, desembolso, movimientos y CxC de caja diario.</summary>
internal static class CreditoCajaDiarioEndpoints
{
    public static void MapCreditoCajaDiarioSesionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/usuarios-no-asignados-caja",
                async Task<Results<Ok<List<UsuariosNoAsignadosCajaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IUsuariosNoAsignadosCajaReadService usuariosNoAsignados,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina v?lida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("UsuariosNoAsignadosCaja");
                    try
                    {
                        var items = await usuariosNoAsignados.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
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
                        log.LogWarning(ex, "oficinaId inv?lido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_UsuariosNoAsignadosCaja");
                        var detail = "No se pudo listar usuarios sin caja asignada.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoUsuariosNoAsignadosCaja")
            .WithSummary(
                "Solo lectura: CREDITO.usp_UsuariosNoAsignadosCaja(OficinaId). oficinaId obligatorio y debe coincidir con vendix:oficina_id del JWT. CreditoUser. Equivale a CajaBL.ListaUsuariosNoAsignado.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<UsuariosNoAsignadosCajaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/obtener-saldo-cuenta-caja-diario",
                async Task<Results<Ok<ObtenerSaldoCuentaCajaDiarioResponse>, ProblemHttpResult>> (
                    int? cajaDiarioId,
                    int? tipoPagoId,
                    IObtenerSaldoCuentaCajaDiarioReadService saldoCuenta,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "cajaDiarioId es obligatorio y debe ser un entero >= 1.");
                    }

                    var tipoPago = tipoPagoId ?? 1;
                    if (tipoPago < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "tipoPagoId debe ser un entero >= 1 cuando se indica.");
                    }

                    var log = loggerFactory.CreateLogger("ObtenerSaldoCuentaCajaDiario");
                    try
                    {
                        var valor = await saldoCuenta.ObtenerAsync(cajaDiarioId.Value, tipoPago, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new ObtenerSaldoCuentaCajaDiarioResponse(valor));
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
                        log.LogWarning(ex, "Par?metros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ObtenerSaldoCuentaCajadiario");
                        var detail = "No se pudo obtener el saldo de la cuenta de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoObtenerSaldoCuentaCajaDiario")
            .WithSummary(
                "Solo lectura: CREDITO.usp_ObtenerSaldoCuentaCajadiario(CajaDiarioId, TipoPagoId). cajaDiarioId obligatorio (>=1). tipoPagoId opcional (default 1, como CajaDiarioBL). CreditoUser. saldo puede ser null si el proc no devuelve filas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ObtenerSaldoCuentaCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/caja-diario-sesion",
                async Task<Results<Ok<CajaDiarioSesionDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ICajaDiarioSesionReadService cajaSesion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene un usuario v?lido (vendix:usuario_id).");
                    }

                    var log = loggerFactory.CreateLogger("CajaDiarioSesion");
                    try
                    {
                        var sesion = await cajaSesion
                            .ObtenerSesionAbiertaAsync(usuarioId, oficinaId, ct)
                            .ConfigureAwait(false);
                        if (sesion is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No hay caja diario abierta asignada al usuario en esta oficina.");
                        }

                        return TypedResults.Ok(sesion);
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
                        log.LogError(ex, "Error al leer sesi?n de caja diario");
                        var detail = "No se pudo obtener la sesi?n de caja diario.";
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
            .WithName("CreditoCajaDiarioSesion")
            .WithSummary(
                "Lectura: caja diario abierta con saldos (paridad cabecera CajaDiario.cshtml). usuarioId y oficinaId = JWT.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioSesionDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    public static void MapCreditoCajaDiarioOperacionEndpoints(this WebApplication app)
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



        app.MapGet(
                "/api/v1/credito/desembolsos-pendientes",
                async Task<Results<Ok<List<DesembolsoPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int personaId,
                    IDesembolsoReadService desembolsoRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    if (personaId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "personaId debe ser >= 0.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("DesembolsosPendientes");
                    try
                    {
                        var items = await desembolsoRead
                            .ListarPendientesAsync(oficinaId, usuarioId, personaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogError(ex, "Error al listar desembolsos pendientes");
                        var detail = "No se pudo listar desembolsos pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoDesembolsosPendientes")
            .WithSummary(
                "Solo lectura: cr?ditos APR pendientes de desembolso (paridad CajaDiarioBL.LstDesembolsoJGrid). personaId=0 filtra por usuario del token.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<DesembolsoPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-desembolso",
                async Task<Results<Ok<ValidarDesembolsoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int cajaDiarioId,
                    int creditoId,
                    IDesembolsoReadService desembolsoRead,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(oficinaId, cajaDiarioId, creditoId);
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
                        .ValidateCajaYCreditoOficinaAsync(
                            oficinaId, cajaDiarioId, creditoId, cajaDiarioOficina, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarDesembolso");
                    try
                    {
                        var response = await desembolsoRead.ValidarAsync(cajaDiarioId, creditoId, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al validar desembolso");
                        var detail = "No se pudo validar el desembolso.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarDesembolso")
            .WithSummary("Lectura previa: paridad CreditoController.ValidarDesembolso (CxC PEN y saldo caja).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarDesembolsoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/realizar-desembolso",
                async Task<Results<Ok<RealizarDesembolsoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RealizarDesembolsoRequest body,
                    IDesembolsoReadService desembolsoRead,
                    IDesembolsoWriteService desembolsoWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
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
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId, body.CajaDiarioId, body.CreditoId, cajaDiarioOficina, creditoOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("RealizarDesembolso");
                    try
                    {
                        var validacion = await desembolsoRead
                            .ValidarAsync(body.CajaDiarioId, body.CreditoId, ct)
                            .ConfigureAwait(false);
                        if (!validacion.PuedeDesembolsar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Desembolso no permitido",
                                detail: validacion.Mensaje ?? "Validaci?n de desembolso fallida.");
                        }

                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await desembolsoWrite
                            .RealizarAsync(body.CajaDiarioId, body.CreditoId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        log.LogWarning(ex, "Cr?dito no encontrado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Desembolso rechazado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Desembolso no permitido",
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
                        log.LogError(ex, "Error al realizar desembolso");
                        var detail = "No se pudo realizar el desembolso.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRealizarDesembolso")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.RealizarDesembolso (UPDATE Credito DES + MovimientoCaja + saldos caja). Idempotente si ya existe movimiento DES.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<RealizarDesembolsoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/entrada-salida-caja-diario",
                async Task<Results<Ok<EntradaSalidaCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EntradaSalidaCajaDiarioRequest body,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IEntradaSalidaCajaDiarioWriteService entradaSalidaWrite,
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

                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            IdRules.RequirePositive(body.PersonaId, "personaId"),
                            IdRules.RequirePositive(body.TipoOperacionId, "tipoOperacionId")));
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

                    var log = loggerFactory.CreateLogger("EntradaSalidaCajaDiario");
                    try
                    {
                        var preError = await CajaCreditoWriteGuards.ValidateEntradaSalidaPrecondicionesAsync(
                            body.CajaDiarioId,
                            body.TipoOperacionId,
                            body.Importe,
                            body.Descripcion,
                            body.TipoPagoId,
                            entradaSalidaRead,
                            ct).ConfigureAwait(false);
                        if (preError is not null)
                        {
                            return preError;
                        }

                        var response = await entradaSalidaWrite
                            .EjecutarAsync(
                                body.CajaDiarioId,
                                body.PersonaId,
                                body.TipoOperacionId,
                                body.Importe,
                                body.Descripcion!.Trim(),
                                usuarioId,
                                body.TipoPagoId,
                                ct)
                            .ConfigureAwait(false);

                        var resultError = CajaCreditoWriteGuards.MapEntradaSalidaResult(response);
                        if (resultError is not null)
                        {
                            return resultError;
                        }

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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_EntradaSalidaCajaDiario");
                        var detail = "No se pudo registrar la entrada/salida de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEntradaSalidaCajaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_EntradaSalidaCajaDiario. Paridad CajaDiarioBL.EntradaSalida / RealizarEntradaSalidaCajaDiario. Param SQL Decripcion (typo legado).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<EntradaSalidaCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cuentas-por-cobrar-pendientes",
                async Task<Results<Ok<List<CuentaPorCobrarPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int cajaDiarioId,
                    int personaId,
                    ICuentaPorCobrarPagoReadService cuentaPorCobrarPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || cajaDiarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y cajaDiarioId deben ser enteros >= 1.");
                    }

                    if (personaId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "personaId debe ser >= 0.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CuentasPorCobrarPendientes");
                    try
                    {
                        var items = await cuentaPorCobrarPago
                            .ListarPendientesAsync(oficinaId, cajaDiarioId, usuarioId, personaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogError(ex, "Error al listar cuentas por cobrar pendientes");
                        var detail = "No se pudo listar cuentas por cobrar pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCuentasPorCobrarPendientes")
            .WithSummary(
                "Solo lectura: paridad CajaDiarioBL.LstCuentasxCobrarJGrid (CxC PEN + orden CON/ENV si personaId > 0). Requiere cajaDiarioId para regla CAJA CENTRAL.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CuentaPorCobrarPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuenta-por-cobrar",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuentaxCobrarRequest body,
                    ICuentaPorCobrarPagoReadService cuentaPorCobrarPagoRead,
                    ICuentaPorCobrarPagoWriteService cuentaPorCobrarPagoWrite,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.CuentaxCobrarId < 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "cuentaxCobrarId debe ser >= 0.");
                    }

                    if (body.CuentaxCobrarId == 0 && body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "ordenVentaId debe ser >= 1 cuando cuentaxCobrarId es 0.");
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

                    var log = loggerFactory.CreateLogger("PagarCuentaxCobrar");
                    try
                    {
                        if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Conflicto",
                                detail: "La caja diario est? cerrada.");
                        }

                        var puedeCobrar = await cuentaPorCobrarPagoRead
                            .PuedeCobrarAsync(body.OficinaId, body.OrdenVentaId, body.CuentaxCobrarId, ct)
                            .ConfigureAwait(false);
                        if (!puedeCobrar)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "La cuenta por cobrar u orden de venta no existe, no est? pendiente o no pertenece a la oficina.");
                        }

                        var response = await cuentaPorCobrarPagoWrite
                            .PagarAsync(body.OrdenVentaId, body.CuentaxCobrarId, body.CajaDiarioId, usuarioId, ct)
                            .ConfigureAwait(false);

                        var resultError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (resultError is not null)
                        {
                            return resultError;
                        }

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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuentaxCobrar");
                        var detail = "No se pudo registrar el pago de cuenta por cobrar.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuentaxCobrar")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuentaxCobrar. Paridad CajaDiarioBL.RealizarPagarCuentaxCobrar. cuentaxCobrarId=0 = orden venta CON.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/credito/cerrar-cajas-diarios",
                async Task<Results<Ok<CajaDiarioOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CerrarCajasDiariosRequest body,
                    ICajaDiarioOperacionWriteService cajaDiarioOperacion,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    if (body.Sobrante < 0m)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "sobrante no puede ser negativo.");
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

                    // Paridad caja chica: no ejecutar USP si aún hay cajas abiertas / nada por cerrar.
                    var validacion = await saldosCierre
                        .ValidarCierreMasivoAsync(body.OficinaId, ct)
                        .ConfigureAwait(false);
                    if (!validacion.PuedeCerrar)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: string.IsNullOrWhiteSpace(validacion.Mensaje)
                                ? "No se puede cerrar/transferir las cajas diarias."
                                : validacion.Mensaje);
                    }

                    var log = loggerFactory.CreateLogger("CerrarCajasDiarios");
                    try
                    {
                        var response = await cajaDiarioOperacion
                            .CerrarCajasDiariosAsync(usuarioId, body.OficinaId, body.Sobrante, ct)
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CerrarCajasDiarios");
                        var detail = "No se pudo cerrar/transferir las cajas diarias de la oficina.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarCajasDiarios")
            .WithSummary(
                "Escritura: CREDITO.usp_CerrarCajasDiarios(UsuarioCierreId, OficinaId, Sobrante). Valida cierre masivo (409) y rechaza sobrante negativo. usuarioCierreId = vendix:usuario_id. Paridad CajaDiarioBL.TransferirCajaDiarioBoveda / SaldosController.Transferir.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
