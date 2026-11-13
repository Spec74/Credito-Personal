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

/// <summary>Caja chica: sesión, movimientos, rendiciones y cierres.</summary>
internal static class CreditoCajaChicaOperacionEndpoints
{
    public static void MapCreditoCajaChicaOperacionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/caja-chica-sesion",
                async Task<Results<Ok<CajaChicaSesionDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CajaChicaSesion");
                    try
                    {
                        var sesion = await cajaChica.GetSesionAbiertaAsync(usuarioId, ct).ConfigureAwait(false);
                        if (sesion is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe caja chica abierta para el usuario.");
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
                        log.LogError(ex, "Error al obtener sesi?n caja chica");
                        var detail = "No se pudo obtener la sesi?n de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCajaChicaSesion")
            .WithSummary("Paridad CajaChicaController.Index / ObtenerCajaChicaDiario.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaChicaSesionDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/movimientos-caja-chica",
                async Task<Results<Ok<List<MovimientoCajaChicaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? tipo,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (tipo is not ("E" or "S"))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "tipo debe ser E (entrada) o S (salida).");
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("MovimientosCajaChica");
                    try
                    {
                        var items = await cajaChica.ListarMovimientosAsync(usuarioId, tipo, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar movimientos caja chica");
                        var detail = "No se pudo listar movimientos de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientosCajaChica")
            .WithSummary("Paridad CajaChicaController.ListarMovimientosJGrid.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<MovimientoCajaChicaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rendiciones-pendientes-caja-chica",
                async Task<Results<Ok<List<RendicionPendienteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("RendicionesPendientesCajaChica");
                    try
                    {
                        var items = await cajaChica.ListarRendicionesPendientesAsync(usuarioId, ct).ConfigureAwait(false);
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar rendiciones pendientes");
                        var detail = "No se pudo listar rendiciones pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRendicionesPendientesCajaChica")
            .WithSummary("Paridad CajaChicaController.ListarRencionesPendientesJGrid.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RendicionPendienteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rendiciones-caja-chica",
                async Task<Results<Ok<List<RendicionComprobanteRowDto>>, ProblemHttpResult>> (
                    int? movimientoCajaChicaId,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoCajaChicaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "movimientoCajaChicaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RendicionesCajaChica");
                    try
                    {
                        var items = await cajaChica
                            .ListarRendicionesAsync(movimientoCajaChicaId.Value, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar rendiciones");
                        var detail = "No se pudo listar comprobantes de rendici?n.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRendicionesCajaChica")
            .WithSummary("Paridad CajaChicaController.ListarRendicionGrd.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RendicionComprobanteRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/tiene-rendiciones-pendientes-caja-chica",
                async Task<Results<Ok<int>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("TieneRendicionesPendientesCajaChica");
                    try
                    {
                        var count = await cajaChica.ContarRendicionesPendientesAsync(usuarioId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(count);
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
                        log.LogError(ex, "Error al contar rendiciones pendientes");
                        var detail = "No se pudo verificar rendiciones pendientes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTieneRendicionesPendientesCajaChica")
            .WithSummary("Paridad CajaChicaController.TieneRencicionesPendientes.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<int>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/monto-caja-chica",
                async Task<Results<Ok<decimal>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioReadService cajaChica,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("MontoCajaChica");
                    try
                    {
                        var saldo = await cajaChica.GetSaldoFinalSesionAbiertaAsync(usuarioId, ct).ConfigureAwait(false);
                        if (saldo is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe caja chica abierta.");
                        }

                        return TypedResults.Ok(saldo.Value);
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
                        log.LogError(ex, "Error al obtener monto caja chica");
                        var detail = "No se pudo obtener el saldo de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMontoCajaChica")
            .WithSummary("Paridad CajaChicaController.MostrarMontoCaja.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<decimal>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/rendiciones-caja-chica",
                async Task<Results<Ok, ProblemHttpResult>> (
                    CrearRendicionCajaChicaRequest body,
                    ICajaChicaRendicionWriteService rendiciones,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            IdRules.RequirePositive(body.MovimientoCajaChicaId, "movimientoCajaChicaId"),
                            IdRules.RequirePositive(body.TipoDocumentoId, "tipoDocumentoId"),
                            MoneyRules.RequirePositive(body.Importe, "importe"),
                            StringRules.RequireText(body.DetalleGasto, "detalleGasto", StringRules.MaxGlosa),
                            StringRules.OptionalMaxLength(body.Serie, "serie", StringRules.MaxSerie),
                            StringRules.OptionalMaxLength(body.Numero, "numero", StringRules.MaxSerie),
                            StringRules.OptionalMaxLength(body.RazonSocial, "razonSocial", StringRules.MaxNombre),
                            string.IsNullOrWhiteSpace(body.Ruc)
                                ? null
                                : StringRules.RequireRuc(body.Ruc)));
                    if (edgeError is not null)
                        return edgeError;

                    var log = loggerFactory.CreateLogger("CrearRendicionCajaChica");
                    try
                    {
                        var mensaje = await rendiciones.CrearAsync(body, ct).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(mensaje))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Rendici?n no registrada",
                                detail: mensaje);
                        }

                        return TypedResults.Ok();
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Solicitud inv?lida");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "La solicitud enviada no es válida." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear rendici?n");
                        var detail = "No se pudo registrar la rendici?n.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCrearRendicionCajaChica")
            .WithSummary("Paridad CajaChicaController.CrearMovimientoRendicion.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapDelete(
                "/api/v1/credito/rendiciones-caja-chica/{rendicionId:int}",
                async Task<Results<Ok, ProblemHttpResult>> (
                    int rendicionId,
                    ICajaChicaRendicionWriteService rendiciones,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (rendicionId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "rendicionId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarRendicionCajaChica");
                    try
                    {
                        await rendiciones.DeleteAsync(rendicionId, ct).ConfigureAwait(false);
                        return TypedResults.Ok();
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
                        log.LogError(ex, "Error al eliminar rendici?n");
                        var detail = "No se pudo eliminar el comprobante.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEliminarRendicionCajaChica")
            .WithSummary("Paridad CajaChicaController.EliminarComprobanteCajaChica.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-rendicion-caja-chica",
                async Task<Results<Ok<CerrarRendicionCajaChicaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int movimientoCajaChicaId,
                    ICajaChicaRendicionWriteService rendiciones,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoCajaChicaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "movimientoCajaChicaId debe ser >= 1.");
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CerrarRendicionCajaChica");
                    try
                    {
                        var (error, result) = await rendiciones
                            .CerrarAsync(movimientoCajaChicaId, usuarioId, ct)
                            .ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo cerrar rendici?n",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al cerrar rendici?n");
                        var detail = "No se pudo cerrar la rendici?n.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarRendicionCajaChica")
            .WithSummary("Paridad CajaChicaController.CerrarRendicion.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CerrarRendicionCajaChicaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/cerrar-caja-chica-diario",
                async Task<Results<Ok<CerrarCajaChicaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICajaChicaDiarioWriteService cajaChicaWrite,
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

                    var log = loggerFactory.CreateLogger("CerrarCajaChicaDiario");
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

                        var (error, result) = await cajaChicaWrite
                            .CerrarSesionAsync(usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo cerrar caja chica",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al cerrar caja chica diario");
                        var detail = "No se pudo cerrar la caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCerrarCajaChicaDiario")
            .WithSummary("Paridad CajaChicaController.CerrarCajaChicaDiario.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CerrarCajaChicaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-saldos-caja-chica-boveda",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirSaldosCajaChicaBovedaRequest body,
                    ICajaChicaDiarioWriteService cajaChicaWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        TesoreriaValidacion.ValidarMovimientoOficina(body.OficinaId, body.Importe, body.Descripcion));
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

                    var log = loggerFactory.CreateLogger("TransferirSaldosCajaChicaBoveda");
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

                        var (error, ok) = await cajaChicaWrite
                            .TransferirSaldosABovedaAsync(
                                body.OficinaId,
                                usuarioId,
                                body.Importe,
                                body.Descripcion,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Transferencia no completada",
                                detail: error);
                        }

                        return TypedResults.Ok(ok);
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
                        log.LogError(ex, "Error al transferir saldos caja chica a b?veda");
                        var detail = "No se pudo transferir a b?veda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirSaldosCajaChicaBoveda")
            .WithSummary("Paridad CajaChicaController.TransferirBoveda / TransferirSaldosBoveda.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
