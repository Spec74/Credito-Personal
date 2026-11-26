using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.MediosPago;
using Credito.Modern.Application.Oficinas;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.MediosPago;

internal static class OficinaMedioPagoEndpoints
{
    public static void MapOficinaMedioPagoEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/oficinas/{oficinaId:int}/medios-pago",
                async Task<Results<Ok<List<OficinaMedioPagoDto>>, ProblemHttpResult>> (
                    int oficinaId,
                    bool? incluirInactivos,
                    bool? soloRaices,
                    HttpContext httpContext,
                    IOficinaMedioPagoService service,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    // Lectura propia: JWT = oficinaId.
                    // Lectura Central (IndPrincipal): permitida a cualquier sucursal autenticada
                    // (solo activos) para armar remesa/fondeo sin exponer CRUD ajeno.
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    var esLecturaCentralRemota = false;
                    if (oficinaError is not null)
                    {
                        var activas = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
                        var target = activas.FirstOrDefault(o => o.OficinaId == oficinaId);
                        if (target?.IndPrincipal == true && incluirInactivos != true)
                        {
                            esLecturaCentralRemota = true;
                        }
                        else
                        {
                            return oficinaError;
                        }
                    }

                    try
                    {
                        var items = await service
                            .ListByOficinaAsync(
                                oficinaId,
                                incluirInactivos == true && !esLecturaCentralRemota,
                                soloRaices != false,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or ArgumentException)
                    {
                        loggerFactory.CreateLogger("OficinaMedioPago").LogWarning(ex, "List medios");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "No se pudieron listar medios",
                            detail: ex.Message);
                    }
                })
            .WithName("OficinaMediosPagoList")
            .WithTags("medios-pago")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<OficinaMedioPagoDto>>();

        app.MapPost(
                "/api/v1/medios-pago/guardar",
                async Task<Results<Ok<OficinaMedioPagoOperacionResponse>, ProblemHttpResult>> (
                    GuardarOficinaMedioPagoRequest body,
                    HttpContext httpContext,
                    IOficinaMedioPagoService service,
                    ILoggerFactory loggerFactory,
                    CancellationToken ct) =>
                {
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    try
                    {
                        var result = await service.GuardarAsync(body, ct).ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or DbException)
                    {
                        loggerFactory.CreateLogger("OficinaMedioPago").LogWarning(ex, "Guardar medio");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "No se pudo guardar el medio",
                            detail: ex.Message);
                    }
                })
            .WithName("OficinaMedioPagoGuardar")
            .WithTags("medios-pago")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OficinaMedioPagoOperacionResponse>();

        app.MapPost(
                "/api/v1/medios-pago/{oficinaMedioPagoId:int}/activar",
                async Task<Results<Ok<OficinaMedioPagoOperacionResponse>, ProblemHttpResult>> (
                    int oficinaMedioPagoId,
                    bool activo,
                    int? oficinaId,
                    HttpContext httpContext,
                    IOficinaMedioPagoService service,
                    ILoggerFactory loggerFactory,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var targetOficinaId = oficinaId is > 0 ? oficinaId.Value : jwtOficinaId;
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, targetOficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    try
                    {
                        var result = await service
                            .SetActivoAsync(oficinaMedioPagoId, targetOficinaId, activo, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or DbException)
                    {
                        loggerFactory.CreateLogger("OficinaMedioPago").LogWarning(ex, "Activar medio");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "No se pudo cambiar el estado",
                            detail: ex.Message);
                    }
                })
            .WithName("OficinaMedioPagoActivar")
            .WithTags("medios-pago")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OficinaMedioPagoOperacionResponse>();
    }
}
