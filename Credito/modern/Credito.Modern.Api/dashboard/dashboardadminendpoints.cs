using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Dashboard;

internal static class DashboardAdminEndpoints
{
    public static void MapDashboardAdminEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/dashboard/admin",
                async Task<Results<Ok<DashboardAdminDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IDashboardAdminReadService dashboard,
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

                    var log = loggerFactory.CreateLogger("DashboardAdmin");
                    try
                    {
                        var dto = await dashboard
                            .ObtenerAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or TimeoutException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("DashboardAdmin")
            .WithSummary("Tablero gerencial de la oficina del JWT (paridad Dashboard/Admin).")
            .WithTags("dashboard")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<DashboardAdminDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/dashboard/admin/shell",
                async Task<Results<Ok<DashboardAdminShellDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IDashboardAdminReadService dashboard,
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

                    var log = loggerFactory.CreateLogger("DashboardAdmin");
                    try
                    {
                        var dto = await dashboard
                            .ObtenerShellAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or TimeoutException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("DashboardAdminShell")
            .WithSummary("KPIs y cartera del tablero gerencial (carga rápida).")
            .WithTags("dashboard")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<DashboardAdminShellDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/dashboard/admin/detalle",
                async Task<Results<Ok<DashboardAdminDetalleDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IDashboardAdminReadService dashboard,
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

                    var log = loggerFactory.CreateLogger("DashboardAdmin");
                    try
                    {
                        var dto = await dashboard
                            .ObtenerDetalleAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or TimeoutException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("DashboardAdminDetalle")
            .WithSummary("Flujo, históricos y analistas del tablero gerencial.")
            .WithTags("dashboard")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<DashboardAdminDetalleDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException)
        {
            log.LogWarning(ex, "Dashboard admin: configuración");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        var isTimeout = ex is TimeoutException
            || (ex is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == -2 || sql.Number == 1222));
        log.LogError(ex, "Dashboard admin");
        var detail = isTimeout
            ? "El tablero tardó demasiado en responder. Reintente; si persiste, revise el plan de Azure SQL / índices."
            : "No se pudieron obtener los indicadores gerenciales.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: isTimeout ? "Tiempo de espera agotado" : "Error de base de datos");
    }
}
