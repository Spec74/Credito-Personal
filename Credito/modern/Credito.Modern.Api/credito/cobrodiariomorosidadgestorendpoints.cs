using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.UsuariosAdmin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

/// <summary>Paridad MVC <c>ReporteMorosidadGestor</c> (cobro diario con mora &gt; 0; gestor opcional).</summary>
internal static class CobroDiarioMorosidadGestorEndpoints
{
    public static void MapCobroDiarioMorosidadGestorEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-morosidad-gestor-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    IRptCobroDiarioReadService rptCobroDiario,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (uid, oid, err) = CobroDiarioReportAccess.ResolveFiltros(
                        httpContext,
                        usuarioId,
                        oficinaId,
                        requiereGestor: false);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptMorosidadGestorCsv");
                    try
                    {
                        var items = CobroDiarioReportAccess.ApplySoloMora(
                            await rptCobroDiario.ListarAsync(uid, oid, ct).ConfigureAwait(false),
                            soloMora: true);
                        var bytes = RptCobroDiarioCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", "morosidad-gestor.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or ArgumentOutOfRangeException)
                    {
                        return ExportError(log, env, ex, "csv");
                    }
                })
            .WithName("CreditoRptMorosidadGestorCsv")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv; charset=utf-8");

        app.MapGet(
                "/api/v1/credito/rpt-morosidad-gestor-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    IRptCobroDiarioReadService rptCobroDiario,
                    IUsuarioAdminReadService usuarios,
                    ICajaMaestroReadService cajas,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (uid, oid, err) = CobroDiarioReportAccess.ResolveFiltros(
                        httpContext,
                        usuarioId,
                        oficinaId,
                        requiereGestor: false);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptMorosidadGestorPdf");
                    try
                    {
                        var items = CobroDiarioReportAccess.ApplySoloMora(
                            await rptCobroDiario.ListarAsync(uid, oid, ct).ConfigureAwait(false),
                            soloMora: true);
                        var pdfContext = await GestorInformePdfContextBuilder
                            .BuildCobroDiarioAsync(items, uid, soloMora: true, usuarios, cajas, ct, oficinas, oid)
                            .ConfigureAwait(false);
                        var bytes = RptCobroDiarioPdfDocument.Build(items, pdfContext, soloMora: true);
                        return TypedResults.File(bytes, "application/pdf", "morosidad-gestor.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or ArgumentOutOfRangeException)
                    {
                        return ExportError(log, env, ex, "pdf");
                    }
                })
            .WithName("CreditoRptMorosidadGestorPdf")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static ProblemHttpResult ExportError(
        ILogger log,
        IHostEnvironment env,
        Exception ex,
        string kind)
    {
        log.LogError(ex, "Error en morosidad gestor {Kind}", kind);
        var detail = $"No se pudo generar el {kind} de morosidad por gestor.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        var status = ex is ArgumentOutOfRangeException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status503ServiceUnavailable;
        return TypedResults.Problem(
            statusCode: status,
            title: status == StatusCodes.Status400BadRequest
                ? "Parámetros inválidos"
                : "Error de base de datos",
            detail: detail);
    }
}
