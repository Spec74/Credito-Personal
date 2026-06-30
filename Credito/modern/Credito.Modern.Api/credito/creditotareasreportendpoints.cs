using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTareas;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

internal static class CreditoTareasReportEndpoints
{
    public static void MapCreditoTareasReportEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-credito-tarea",
                async Task<Results<Ok<List<TareaReporteRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? estado,
                    ITareasReadService tareas,
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

                    var log = loggerFactory.CreateLogger("RptCreditoTarea");
                    try
                    {
                        var items = await tareas
                            .ListarParaReporteAsync(usuarioId, oficinaId, estado, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptCreditoTarea")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<TareaReporteRowDto>>();

        app.MapGet(
                "/api/v1/credito/rpt-credito-tarea-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? estado,
                    ITareasReadService tareas,
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

                    var log = loggerFactory.CreateLogger("RptCreditoTareaCsv");
                    try
                    {
                        var items = await tareas
                            .ListarParaReporteAsync(usuarioId, oficinaId, estado, ct)
                            .ConfigureAwait(false);
                        var bytes = TareaReporteCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", "credito-tareas.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptCreditoTareaCsv")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-credito-tarea-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? estado,
                    ITareasReadService tareas,
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

                    var log = loggerFactory.CreateLogger("RptCreditoTareaPdf");
                    try
                    {
                        var items = await tareas
                            .ListarParaReporteAsync(usuarioId, oficinaId, estado, ct)
                            .ConfigureAwait(false);
                        var grupos = TareaReporteBuilder.AgruparPorCliente(items);
                        var pdfBytes = TareaReportePdfDocument.Generar(grupos, estado, items.Count);
                        return TypedResults.File(pdfBytes, "application/pdf", "credito-tareas.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptCreditoTareaPdf")
            .WithTags("credito", "tareas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Rpt crédito tarea: configuración");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Rpt crédito tarea");
        var detail = "No se pudo generar el reporte de tareas.";
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
