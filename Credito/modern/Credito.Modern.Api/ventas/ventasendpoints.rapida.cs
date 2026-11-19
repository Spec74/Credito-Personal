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

/// <summary>Venta rápida / pedido.</summary>
internal static partial class VentasEndpoints
{
    private static void MapVentasRapida(WebApplication app)
    {
        app.MapGet(
                "/api/v1/ventas/caja-diario-venta-rapida",
                async Task<Results<Ok<CajaDiarioVentaRapidaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IVentaRapidaCajaDiarioReadService cajaDiarioVentaRapida,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
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
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    var log = loggerFactory.CreateLogger("CajaDiarioVentaRapida");
                    try
                    {
                        var caja = await cajaDiarioVentaRapida
                            .ObtenerAbiertaPorUsuarioAsync(usuarioId, oficinaId, ct)
                            .ConfigureAwait(false);
                        if (caja is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No hay caja diario abierta asignada al usuario en esta oficina.");
                        }

                        return TypedResults.Ok(caja);
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
                        log.LogError(ex, "Error al leer caja diario venta rápida");
                        var detail = "No se pudo obtener la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasCajaDiarioVentaRapida")
            .WithSummary(
                "Lectura: paridad VentaRapidaController.Index (caja abierta). usuarioId = JWT; oficinaId = JWT; filtra Caja.OficinaId (restricción adicional frente al MVC).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioVentaRapidaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/articulo-venta-rapida",
                async Task<Results<Ok<ArticuloVentaRapidaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    string codigo,
                    IVentaRapidaReadService ventaRapidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(codigo))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "codigo es obligatorio.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ArticuloVentaRapida");
                    try
                    {
                        var articulo = await ventaRapidaRead.ObtenerPorCodigoAsync(codigo, ct).ConfigureAwait(false);
                        if (articulo is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No hay artículo activo con ese código, o no tiene precio en lista. Créelo en Maestros y asigne precio.");
                        }

                        return TypedResults.Ok(articulo);
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
                        log.LogError(ex, "Error al buscar artículo venta rápida");
                        var detail = "No se pudo obtener el artículo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasArticuloVentaRapida")
            .WithSummary(
                "Lectura: paridad VentaRapidaController.ObtenerArticulo. codigo = CodArticulo; stock = series EN_ALMACEN (EstadoId 2). oficinaId = JWT.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ArticuloVentaRapidaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/realizar-pedido",
                async Task<Results<Ok<RealizarPedidoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RealizarPedidoRequest body,
                    IVentaRapidaWriteService ventaRapida,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
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

                    if (body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "personaId debe ser >= 1.");
                    }

                    if (body.Pedidos is null || body.Pedidos.Count == 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "pedidos debe contener al menos una línea.");
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

                    var log = loggerFactory.CreateLogger("RealizarPedido");
                    try
                    {
                        if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Conflicto",
                                detail: "La caja diario está cerrada.");
                        }

                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var (error, result) = await ventaRapida
                            .RealizarPedidoAsync(
                                body.OficinaId,
                                body.CajaDiarioId,
                                body.PersonaId,
                                usuarioId,
                                serverTime.Value,
                                body.Pedidos,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Venta no completada",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al realizar pedido venta rápida");
                        var detail = "No se pudo completar la venta rápida.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRealizarPedido")
            .WithSummary(
                "Escritura: paridad OrdenVentaBL.RealizarPedido / VentaRapidaController. Orden CON+ENV, detalle, series y usp_PagarCuentaxCobrar en una transacción.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RealizarPedidoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }
}