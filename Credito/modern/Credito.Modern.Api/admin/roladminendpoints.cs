using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.RolAdmin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Admin;

internal static class RolAdminEndpoints
{
    public static void MapRolAdminEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/roles/gestion",
                async Task<Results<Ok<List<RolGestionListItemDto>>, ProblemHttpResult>> (
                    bool? incluirInactivos,
                    IRolAdminReadService roles,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("RolesGestion");
                    try
                    {
                        var items = await roles
                            .ListGestionAsync(incluirInactivos == true, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("RolesGestion")
            .WithTags("roles-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RolGestionListItemDto>>();

        app.MapGet(
                "/api/v1/roles/{rolId:int}/menus-detalle",
                async Task<Results<Ok<RolMenusDetalleDto>, NotFound, ProblemHttpResult>> (
                    int rolId,
                    IRolAdminReadService roles,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (rolId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "rolId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RolMenusDetalle");
                    try
                    {
                        var detalle = await roles.GetMenusDetalleAsync(rolId, ct).ConfigureAwait(false);
                        return detalle is null ? TypedResults.NotFound() : TypedResults.Ok(detalle);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("RolMenusDetalle")
            .WithTags("roles-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RolMenusDetalleDto>();

        app.MapPost(
                "/api/v1/roles/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarRolRequest body,
                    IRolAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Denominacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion es obligatoria.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "GuardarRol",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarRol")
            .WithTags("roles-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/roles/{rolId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int rolId,
                    IRolAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (rolId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "rolId debe ser >= 1.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "ActivarRol",
                        ct,
                        token => write.ActivarAsync(rolId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarRol")
            .WithTags("roles-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/roles/{rolId:int}/asignar-menus",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int rolId,
                    AsignarRolMenusRequest body,
                    IRolAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (rolId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "rolId debe ser >= 1.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "AsignarRolMenus",
                        ct,
                        token => write.AsignarMenusAsync(rolId, body.MenuIds, token)).ConfigureAwait(false);
                })
            .WithName("AsignarRolMenus")
            .WithTags("roles-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MaestroOperacionResponse>();
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Cadena de conexión no configurada");
            return TypedResults.Problem(
                detail: ioe.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Error en roles admin");
        var detail = "No se pudo completar la operación.";
        if (env.IsDevelopment())
            detail += $" Detalle: {ex.Message}";
        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }

    private static async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> WriteAsync(
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        string operation,
        CancellationToken ct,
        Func<CancellationToken, Task<MaestroOperacionResponse>> action)
    {
        var log = loggerFactory.CreateLogger(operation);
        try
        {
            var response = await action(ct).ConfigureAwait(false);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Cadena de conexión no configurada");
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }
        catch (DbException ex)
        {
            log.LogError(ex, "Error en {Operation}", operation);
            var detail = "No se pudo completar la operación.";
            if (env.IsDevelopment())
                detail += $" Detalle: {ex.Message}";
            return TypedResults.Problem(
                detail: detail,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Error de base de datos");
        }
    }
}
