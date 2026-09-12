using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

internal static class CreditoCondonacionEndpoints
{
    public static void MapCreditoCondonacionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/condonaciones-pendientes",
                async Task<Results<Ok<IReadOnlyList<CondonacionPendienteDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ICreditoCondonacionService service,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:oficina_id.");
                    }

                    var log = loggerFactory.CreateLogger("CondonacionesPendientes");
                    try
                    {
                        var dto = await service.ListarPendientesAsync(oficinaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return WriteError(log, env, ex, "No se pudieron listar las solicitudes de condonación.");
                    }
                })
            .WithName("CreditoCondonacionesPendientes")
            .WithSummary("Bandeja de solicitudes no aprobadas de la oficina del JWT.")
            .WithTags("credito-condonacion")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<IReadOnlyList<CondonacionPendienteDto>>(StatusCodes.Status200OK, "application/json");

        app.MapGet(
                "/api/v1/credito/condonacion-pendiente",
                async Task<Results<Ok<CondonacionPendienteCreditoDto>, ProblemHttpResult>> (
                    int creditoId,
                    HttpContext httpContext,
                    ICreditoCondonacionService service,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "creditoId debe ser >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:oficina_id.");
                    }

                    var log = loggerFactory.CreateLogger("CondonacionPendienteCredito");
                    try
                    {
                        var dto = await service
                            .ObtenerPendientePorCreditoAsync(oficinaId, creditoId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return WriteError(log, env, ex, "No se pudo consultar la solicitud de condonación.");
                    }
                })
            .WithName("CreditoCondonacionPendiente")
            .WithTags("credito-condonacion")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CondonacionPendienteCreditoDto>();

        app.MapPost(
                "/api/v1/credito/solicitar-condonacion",
                async Task<Results<Ok<SolicitarCondonacionResponse>, ProblemHttpResult>> (
                    SolicitarCondonacionRequest body,
                    HttpContext httpContext,
                    ICreditoCondonacionService service,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId)
                        || body.OficinaId != oficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con vendix:oficina_id.");
                    }

                    var log = loggerFactory.CreateLogger("SolicitarCondonacion");
                    try
                    {
                        var dto = await service
                            .SolicitarAsync(body with { OficinaId = oficinaId }, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: ex.Message);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return WriteError(log, env, ex, "No se pudo registrar la solicitud de condonación.");
                    }
                })
            .WithName("CreditoSolicitarCondonacion")
            .WithSummary("Ejecuta CREDITO.usp_SolicitarCondonacion (caja abierta de la oficina).")
            .WithTags("credito-condonacion")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<SolicitarCondonacionResponse>();

        app.MapDelete(
                "/api/v1/credito/condonaciones-pendientes/{id:int}",
                async Task<Results<NoContent, ProblemHttpResult>> (
                    int id,
                    HttpContext httpContext,
                    ICreditoCondonacionService service,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:oficina_id.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarCondonacion");
                    try
                    {
                        await service.EliminarAsync(oficinaId, id, ct).ConfigureAwait(false);
                        return TypedResults.NoContent();
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: ex.Message);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return WriteError(log, env, ex, "No se pudo eliminar la solicitud de condonación.");
                    }
                })
            .WithName("CreditoEliminarCondonacionPendiente")
            .WithTags("credito-condonacion")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static ProblemHttpResult WriteError(ILogger log, IHostEnvironment env, Exception ex, string fallback)
    {
        if (ex is InvalidOperationException)
        {
            log.LogWarning(ex, "Condonación: operación rechazada");
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "Operación no permitida");
        }

        log.LogError(ex, "Condonación");
        var detail = fallback;
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }
}
