using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

internal static class CierreGerencialEndpoints
{
    public static void MapCierreGerencialEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/cierre-gerencial/permisos",
                Results<Ok<CierreGerencialPermisosResponse>, ProblemHttpResult> (
                    HttpContext httpContext,
                    ICierreGerencialAccessService access) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    return TypedResults.Ok(new CierreGerencialPermisosResponse(
                        access.PuedeConsultar(usuarioId, roles),
                        access.PuedeGestionarMetas(usuarioId, roles)));
                })
            .WithName("CierreGerencialPermisos")
            .WithTags("cierre-gerencial")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CierreGerencialPermisosResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapGet(
                "/api/v1/cierre-gerencial/avance",
                async Task<Results<Ok<CierreGerencialAvanceResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime periodo,
                    ICierreGerencialAccessService access,
                    ICierreGerencialReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    if (!access.PuedeConsultar(usuarioId, roles))
                    {
                        return ForbiddenConsulta();
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Oficina no válida",
                            detail: "El token no contiene una oficina válida.");
                    }

                    var log = loggerFactory.CreateLogger("CierreGerencialAvance");
                    try
                    {
                        var filas = await read.ObtenerAvanceAsync(periodo, oficinaId, ct).ConfigureAwait(false);
                        var periodoIso = PrimerDia(periodo).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        return TypedResults.Ok(new CierreGerencialAvanceResponse(
                            periodoIso,
                            filas.Count > 0
                                ? filas[0].FechaCalculo.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                                : null,
                            filas.Count > 0 && filas[0].AvanceNoOficial,
                            filas.Count,
                            filas));
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (SqlException ex)
                    {
                        log.LogError(ex, "Error SQL al obtener avance gerencial");
                        return SqlConflict(ex, env);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error DB al obtener avance gerencial");
                        return DbUnavailable(ex, env);
                    }
                })
            .WithName("CierreGerencialAvance")
            .WithSummary("Paridad CierreGerencialController.ObtenerAvance → usp_ObtenerAvanceMetasGerenciales.")
            .WithTags("cierre-gerencial")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CierreGerencialAvanceResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/cierre-gerencial/metas",
                async Task<Results<Ok<CierreGerencialMetasResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime periodo,
                    ICierreGerencialAccessService access,
                    ICierreGerencialReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    if (!access.PuedeConsultar(usuarioId, roles))
                    {
                        return ForbiddenConsulta();
                    }

                    var log = loggerFactory.CreateLogger("CierreGerencialMetas");
                    try
                    {
                        var metas = await read.ListarMetasAsync(periodo, ct).ConfigureAwait(false);
                        var puedeEditar =
                            access.PuedeGestionarMetas(usuarioId, roles) &&
                            metas.Count > 0 &&
                            metas[0].PuedeEditar;

                        return TypedResults.Ok(new CierreGerencialMetasResponse(
                            PrimerDia(periodo).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            metas.Count > 0 && metas[0].PeriodoCerrado,
                            puedeEditar,
                            metas.Count > 0
                                ? metas[0].FechaLimiteEdicion.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                                : null,
                            metas));
                    }
                    catch (SqlException ex)
                    {
                        log.LogError(ex, "Error SQL al listar metas gerenciales");
                        return SqlConflict(ex, env);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error DB al listar metas gerenciales");
                        return DbUnavailable(ex, env);
                    }
                })
            .WithName("CierreGerencialMetas")
            .WithSummary("Paridad CierreGerencialController.ListarMetas → usp_ListarMetasGerencialesDefinitivas.")
            .WithTags("cierre-gerencial")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CierreGerencialMetasResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/cierre-gerencial/excel",
                async Task<Results<FileContentHttpResult, ProblemHttpResult, NotFound<string>>> (
                    HttpContext httpContext,
                    DateTime periodo,
                    ICierreGerencialAccessService access,
                    ICierreGerencialReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    if (!access.PuedeConsultar(usuarioId, roles))
                    {
                        return ForbiddenConsulta();
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Oficina no válida",
                            detail: "El token no contiene una oficina válida.");
                    }

                    var log = loggerFactory.CreateLogger("CierreGerencialExcel");
                    try
                    {
                        var periodoNormalizado = PrimerDia(periodo);
                        var filas = await read.ObtenerAvanceAsync(periodoNormalizado, oficinaId, ct)
                            .ConfigureAwait(false);
                        if (filas.Count == 0)
                        {
                            return TypedResults.NotFound("No existen datos gerenciales para el periodo solicitado.");
                        }

                        var bytes = CierreGerencialXlsxFormatter.ToXlsx(filas);
                        var estado = filas[0].AvanceNoOficial ? "AVANCE_NO_OFICIAL" : "CIERRE_OFICIAL";
                        var nombre =
                            $"VENDIX_{estado}_{periodoNormalizado:yyyy_MM}_{filas[0].FechaCalculo:yyyyMMdd_HHmm}.xlsx";

                        return TypedResults.File(
                            bytes,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nombre);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Sin datos",
                            detail: ex.Message);
                    }
                    catch (SqlException ex)
                    {
                        log.LogError(ex, "Error SQL al exportar Excel gerencial");
                        return SqlConflict(ex, env);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error DB al exportar Excel gerencial");
                        return DbUnavailable(ex, env);
                    }
                })
            .WithName("CierreGerencialExcel")
            .WithSummary("Paridad CierreGerencialController.ExportarExcel (ClosedXML).")
            .WithTags("cierre-gerencial")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/cierre-gerencial/metas",
                async Task<Results<Ok<object>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarMetasGerencialesRequest body,
                    ICierreGerencialAccessService access,
                    ICierreGerencialWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryUsuarioYRoles(httpContext, out var usuarioId, out var roles, out var problem))
                    {
                        return problem!;
                    }

                    if (!access.PuedeGestionarMetas(usuarioId, roles))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Sin permiso",
                            detail: "Su usuario no está autorizado para registrar o modificar las metas.");
                    }

                    var log = loggerFactory.CreateLogger("CierreGerencialGuardarMetas");
                    try
                    {
                        await write.GuardarMetasAsync(body.Periodo, body.Metas, usuarioId, ct)
                            .ConfigureAwait(false);

                        return TypedResults.Ok<object>(new
                        {
                            success = true,
                            message = "Las metas fueron guardadas correctamente.",
                            periodo = PrimerDia(body.Periodo).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            total = body.Metas?.Count ?? 0,
                        });
                    }
                    catch (ArgumentException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (SqlException ex)
                    {
                        log.LogError(ex, "Error SQL al guardar metas gerenciales");
                        return SqlConflict(ex, env);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error DB al guardar metas gerenciales");
                        return DbUnavailable(ex, env);
                    }
                })
            .WithName("CierreGerencialGuardarMetas")
            .WithSummary("Paridad CierreGerencialController.GuardarMetas → usp_GuardarMetaGerencialDefinitiva.")
            .WithTags("cierre-gerencial")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static bool TryUsuarioYRoles(
        HttpContext httpContext,
        out int usuarioId,
        out List<string> roles,
        out ProblemHttpResult? problem)
    {
        roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out usuarioId))
        {
            problem = TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sesión inválida",
                detail: "La sesión del usuario ha expirado.");
            return false;
        }

        problem = null;
        return true;
    }

    private static ProblemHttpResult ForbiddenConsulta() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Sin permiso",
            detail: "Su usuario no está autorizado para consultar el módulo gerencial.");

    private static ProblemHttpResult SqlConflict(SqlException ex, IHostEnvironment env)
    {
        var detail = ex.Number is >= 50100 and <= 50699
            ? ex.Message
            : "La base de datos no pudo procesar la operación gerencial.";
        if (env.IsDevelopment() && ex.Number is < 50100 or > 50699)
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflicto de base de datos",
            detail: detail);
    }

    private static ProblemHttpResult DbUnavailable(DbException ex, IHostEnvironment env)
    {
        var detail = "No se pudo completar la operación gerencial.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos",
            detail: detail);
    }

    private static DateTime PrimerDia(DateTime periodo) =>
        new(periodo.Year, periodo.Month, 1);
}
