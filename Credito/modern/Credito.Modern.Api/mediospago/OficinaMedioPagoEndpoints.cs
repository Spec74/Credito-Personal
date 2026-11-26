using System.Data.Common;
using System.Security.Claims;
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
                    // Admin de red (ADMIN + Central): cualquier sede (monitoreo / soporte).
                    // Sucursal: puede leer catálogo activo de Central (remesa/fondeo).
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    var esLecturaCentralRemota = false;
                    if (oficinaError is not null)
                    {
                        var (adminRed, targetOk, targetEsCentral) = await ResolverAlcanceMediosAsync(
                                httpContext,
                                oficinaId,
                                oficinas,
                                ct)
                            .ConfigureAwait(false);
                        if (adminRed && targetOk)
                        {
                            // Admin de red: lectura completa.
                        }
                        else if (targetEsCentral && incluirInactivos != true)
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
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    CancellationToken ct) =>
                {
                    var oficinaError = await ValidateMediosWriteOficinaAsync(
                            httpContext,
                            body.OficinaId,
                            oficinas,
                            ct)
                        .ConfigureAwait(false);
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
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<OficinaMedioPagoOperacionResponse>();

        app.MapPost(
                "/api/v1/medios-pago/{oficinaMedioPagoId:int}/activar",
                async Task<Results<Ok<OficinaMedioPagoOperacionResponse>, ProblemHttpResult>> (
                    int oficinaMedioPagoId,
                    bool activo,
                    int? oficinaId,
                    HttpContext httpContext,
                    IOficinaMedioPagoService service,
                    IOficinaReadService oficinas,
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
                    var oficinaError = await ValidateMediosWriteOficinaAsync(
                            httpContext,
                            targetOficinaId,
                            oficinas,
                            ct)
                        .ConfigureAwait(false);
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
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<OficinaMedioPagoOperacionResponse>();
    }

    /// <summary>
    /// Escritura: propia sede, o cualquier sede si admin de red (ADMIN + Central).
    /// </summary>
    private static async Task<ProblemHttpResult?> ValidateMediosWriteOficinaAsync(
        HttpContext httpContext,
        int oficinaId,
        IOficinaReadService oficinas,
        CancellationToken ct)
    {
        var propia = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
        if (propia is null)
        {
            return null;
        }

        var (adminRed, targetOk, _) = await ResolverAlcanceMediosAsync(
                httpContext,
                oficinaId,
                oficinas,
                ct)
            .ConfigureAwait(false);
        if (adminRed && targetOk)
        {
            return null;
        }

        return propia;
    }

    private static async Task<(bool AdminRed, bool TargetOk, bool TargetEsCentral)> ResolverAlcanceMediosAsync(
        HttpContext httpContext,
        int oficinaId,
        IOficinaReadService oficinas,
        CancellationToken ct)
    {
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(static c => c.Value);
        var activas = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
        var sesion = MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficina)
            ? activas.FirstOrDefault(o => o.OficinaId == jwtOficina)
            : null;
        var adminRed = CreditoAuthorizationPolicies.HasAdministrador(roles)
            && sesion?.IndPrincipal == true;
        var target = activas.FirstOrDefault(o => o.OficinaId == oficinaId);
        return (adminRed, target is not null, target?.IndPrincipal == true);
    }
}
