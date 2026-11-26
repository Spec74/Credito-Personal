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
using Credito.Modern.Application.MediosPago;
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

/// <summary>Caja diario — desembolso, E/S y CxC.</summary>
internal static partial class CreditoCajaDiarioEndpoints
{
    private static void MapCajaDiarioDesembolsoYCxc(WebApplication app)
    {
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
                        log.LogWarning(ex, "Crédito no encontrado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se encontró el crédito o la caja indicada."
                                : ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Desembolso rechazado");
                        var detail = string.IsNullOrWhiteSpace(ex.Message)
                            ? "No se pudo realizar el desembolso."
                            : ex.Message.Trim();
                        // Connection misconfig → 503; negocio (estado AP1/APR, etc.) → 409.
                        var isConfig = detail.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
                            || detail.Contains("Configure CreditoDatabase", StringComparison.OrdinalIgnoreCase);
                        return TypedResults.Problem(
                            statusCode: isConfig
                                ? StatusCodes.Status503ServiceUnavailable
                                : StatusCodes.Status409Conflict,
                            title: isConfig ? "Configuración incompleta" : "Desembolso no permitido",
                            detail: detail);
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
                    IOficinaMedioPagoService mediosPago,
                    IOficinaReadService oficinas,
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

                    var (sesionTipo, sesionTipoErr) = await SesionOficina
                        .ResolverAsync(httpContext, oficinas, null, ct)
                        .ConfigureAwait(false);
                    if (sesionTipoErr is not null)
                        return sesionTipoErr;

                    var tipoPagoEs = body.TipoPagoId < 1 ? 1 : body.TipoPagoId;
                    var medioError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(tipoPagoEs, sesionTipo!.EsCentral)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(
                                body.OficinaId,
                                tipoPagoEs,
                                ct)
                            .ConfigureAwait(false));
                    if (medioError is not null)
                        return medioError;

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