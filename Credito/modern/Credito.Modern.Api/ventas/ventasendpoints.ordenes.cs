using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Credito;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Api.Reportes;
using Credito.Modern.Api.Validation;
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
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.Ventas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Ventas;

/// <summary>Órdenes de venta CRUD/envío.</summary>
internal static partial class VentasEndpoints
{
    private static void MapVentasOrdenes(WebApplication app)
    {
        app.MapGet(
                "/api/v1/ventas/ordenes-venta",
                async Task<Results<Ok<OrdenVentaListPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    bool entregado,
                    string? buscar,
                    int page,
                    int pageSize,
                    IOrdenVentaReadService ordenVentaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ListarOrdenesVenta");
                    try
                    {
                        var response = await ordenVentaRead
                            .ListarAsync(oficinaId, entregado, buscar, page < 1 ? 1 : page, pageSize < 1 ? 25 : pageSize, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al listar órdenes de venta");
                        var detail = "No se pudo listar órdenes de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasListarOrdenesVenta")
            .WithSummary("Paridad OrdenVentaController.ListarOrdenesVentaJgrid.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaListPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/orden-venta/{ordenVentaId:int}",
                async Task<Results<Ok<OrdenVentaDetalleResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int ordenVentaId,
                    int oficinaId,
                    IOrdenVentaReadService ordenVentaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || ordenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ObtenerOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaRead.ObtenerDetalleAsync(ordenVentaId, ct).ConfigureAwait(false);
                        if (response is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe la orden de venta indicada.");
                        }

                        if (response.Cabecera.OficinaId != oficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "La orden de venta no pertenece a la oficina del token JWT.");
                        }

                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al obtener orden de venta");
                        var detail = "No se pudo obtener la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasObtenerOrdenVentaDetalle")
            .WithSummary("Paridad OrdenVentaController.BuscarOrdenVentaDet.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaDetalleResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/crear-orden-venta",
                async Task<Results<Ok<CrearOrdenVentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearOrdenVentaRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
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

                    var log = loggerFactory.CreateLogger("CrearOrdenVenta");
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

                        var response = await ordenVentaWrite
                            .CrearAsync(
                                body.OficinaId,
                                body.PersonaId,
                                usuarioId,
                                serverTime.Value,
                                body.TipoVenta ?? "CON",
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al crear orden de venta");
                        var detail = "No se pudo crear la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasCrearOrdenVenta")
            .WithSummary("Paridad OrdenVentaController.Index (id=0, pPersonaId>0).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CrearOrdenVentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/agregar-orden-venta-detalle",
                async Task<Results<Ok<OrdenVentaOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AgregarOrdenVentaDetalleRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaOficinaReadService ordenVentaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1 || string.IsNullOrWhiteSpace(body.NumeroSerie))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, ordenVentaId y numeroSerie son obligatorios.");
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

                    var ordenOid = await ordenVentaOficina
                        .GetOficinaIdByOrdenVentaIdAsync(body.OrdenVentaId, ct)
                        .ConfigureAwait(false);
                    if (ordenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (ordenOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("AgregarOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaWrite
                            .AgregarDetalleAsync(body.OrdenVentaId, body.NumeroSerie, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVentaDet_Ins");
                        var detail = "No se pudo agregar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasAgregarOrdenVentaDetalle")
            .WithSummary("Escritura: VENTAS.usp_OrdenVentaDet_Ins. Paridad OrdenVentaDetBL.AgregarOrdenVentaDetalle.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/actualizar-orden-venta-detalle",
                async Task<Results<Ok<OrdenVentaOperacionResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarOrdenVentaDetalleRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaDetScopeReadService ordenVentaDetScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaDetId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaDetId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scope = await ordenVentaDetScope.GetScopeByDetIdAsync(body.OrdenVentaDetId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el detalle de orden de venta indicado.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El detalle no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ActualizarOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaWrite
                            .ActualizarDetalleAsync(body.OrdenVentaDetId, body.Descuento, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVentaDet_update");
                        var detail = "No se pudo actualizar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasActualizarOrdenVentaDetalle")
            .WithSummary("Escritura: VENTAS.usp_OrdenVentaDet_update. Paridad OrdenVentaDetBL.ActualizarOrdenVentaDetalle.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/eliminar-orden-venta-detalle",
                async Task<Results<Ok<OrdenVentaOperacionResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarOrdenVentaDetalleRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaDetScopeReadService ordenVentaDetScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaDetId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaDetId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scope = await ordenVentaDetScope.GetScopeByDetIdAsync(body.OrdenVentaDetId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el detalle de orden de venta indicado.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El detalle no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaWrite
                            .EliminarDetalleAsync(body.OrdenVentaDetId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVenta_Del (detalle)");
                        var detail = "No se pudo eliminar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEliminarOrdenVentaDetalle")
            .WithSummary("Escritura: VENTAS.usp_OrdenVenta_Del(0, detId). Paridad OrdenVentaDetBL.EliminarOrdenVentaDetalle.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/eliminar-orden-venta",
                async Task<Results<Ok<OrdenVentaOperacionResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarOrdenVentaRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaOficinaReadService ordenVentaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var ordenOid = await ordenVentaOficina
                        .GetOficinaIdByOrdenVentaIdAsync(body.OrdenVentaId, ct)
                        .ConfigureAwait(false);
                    if (ordenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (ordenOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarOrdenVenta");
                    try
                    {
                        var response = await ordenVentaWrite.EliminarOrdenAsync(body.OrdenVentaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVenta_Del");
                        var detail = "No se pudo eliminar la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEliminarOrdenVenta")
            .WithSummary("Escritura: VENTAS.usp_OrdenVenta_Del. Paridad OrdenVentaBL.EliminarOrdenVenta.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/enviar-orden-venta-contado",
                async Task<Results<Ok<EnviarOrdenVentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EnviarOrdenVentaRequest body,
                    IOrdenVentaEnvioReadService ordenEnvioRead,
                    IOrdenVentaEnvioWriteService ordenEnvioWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
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

                    var orden = await ordenEnvioRead.GetOrdenAsync(body.OrdenVentaId, ct).ConfigureAwait(false);
                    if (orden is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (orden.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    if (string.Equals(orden.Estado, "ENV", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La orden de venta ya fue enviada.");
                    }

                    var log = loggerFactory.CreateLogger("EnviarOrdenVentaContado");
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

                        var response = await ordenEnvioWrite
                            .EnviarContadoAsync(body.OrdenVentaId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al enviar orden de venta contado");
                        var detail = "No se pudo enviar la orden de venta al contado.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEnviarOrdenVentaContado")
            .WithSummary("Escritura EF: paridad OrdenVentaBL.EnviarOrdenVentaContado (Estado ENV, TipoVenta CON).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<EnviarOrdenVentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/enviar-orden-venta-credito",
                async Task<Results<Ok<EnviarOrdenVentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EnviarOrdenVentaRequest body,
                    IOrdenVentaEnvioReadService ordenEnvioRead,
                    IOrdenVentaEnvioWriteService ordenEnvioWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
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

                    var orden = await ordenEnvioRead.GetOrdenAsync(body.OrdenVentaId, ct).ConfigureAwait(false);
                    if (orden is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (orden.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    if (string.Equals(orden.Estado, "ENV", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La orden de venta ya fue enviada.");
                    }

                    if (await ordenEnvioRead.ExisteCreditoVinculadoAsync(body.OrdenVentaId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "Ya existe un crédito vinculado a esta orden de venta.");
                    }

                    var log = loggerFactory.CreateLogger("EnviarOrdenVentaCredito");
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

                        var descripciones = await ordenEnvioRead
                            .ListarDescripcionesDetalleActivasAsync(body.OrdenVentaId, ct)
                            .ConfigureAwait(false);

                        var response = await ordenEnvioWrite
                            .EnviarCreditoAsync(
                                body.OrdenVentaId,
                                body.OficinaId,
                                orden.PersonaId,
                                orden.TotalNeto,
                                descripciones,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al enviar orden de venta crédito");
                        var detail = "No se pudo enviar la orden de venta a crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEnviarOrdenVentaCredito")
            .WithSummary(
                "Escritura EF: paridad OrdenVentaBL.EnviarOrdenVentaCredito (INSERT crédito CRE + orden ENV/CRE).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<EnviarOrdenVentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



    }
}