using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Dashboard;

internal static class DashboardAnalistaEndpoints
{
    public static void MapDashboardAnalistaEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/dashboard/analista",
                async Task<Results<Ok<DashboardAnalistaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IDashboardAnalistaReadService dashboard,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId)
                        || !MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:usuario_id y vendix:oficina_id.");
                    }

                    var log = loggerFactory.CreateLogger("DashboardAnalista");
                    try
                    {
                        var dto = await dashboard
                            .ObtenerAsync(usuarioId, oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("DashboardAnalista")
            .WithSummary("Indicadores personales del analista autenticado (oficina del JWT).")
            .WithTags("dashboard")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolPrendario)
            .Produces<DashboardAnalistaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Dashboard analista: configuración");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Dashboard analista");
        var detail = "No se pudieron obtener los indicadores del analista.";
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
