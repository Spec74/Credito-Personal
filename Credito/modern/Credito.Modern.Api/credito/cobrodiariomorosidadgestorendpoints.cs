using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

/// <summary>Paridad MVC <c>ReporteMorosidadGestor</c> (= cobro diario filtrado por mora).</summary>
internal static class CobroDiarioMorosidadGestorEndpoints
{
    public static void MapCobroDiarioMorosidadGestorEndpoints(this WebApplication app)
    {
        MapExport(app, "csv", "text/csv; charset=utf-8", "morosidad-gestor.csv", ExportCsv);
        MapExport(app, "pdf", "application/pdf", "morosidad-gestor.pdf", ExportPdf);
    }

    private static void MapExport(
        WebApplication app,
        string kind,
        string contentType,
        string fileName,
        Func<IRptCobroDiarioReadService, List<RptCobroDiarioRowDto>, byte[]> export)
    {
        app.MapGet(
                $"/api/v1/credito/rpt-morosidad-gestor-{kind}",
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
                        oficinaId);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger($"RptMorosidadGestor{kind}");
                    try
                    {
                        var items = CobroDiarioReportAccess.ApplySoloMora(
                            await rptCobroDiario.ListarAsync(uid, oid, ct).ConfigureAwait(false),
                            soloMora: true);
                        var bytes = export(rptCobroDiario, items);
                        return TypedResults.File(bytes, contentType, fileDownloadName: fileName);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException or ArgumentOutOfRangeException)
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
                })
            .WithName($"CreditoRptMorosidadGestor{kind}")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: contentType);
    }

    private static byte[] ExportCsv(IRptCobroDiarioReadService _, List<RptCobroDiarioRowDto> items) =>
        RptCobroDiarioCsvFormatter.ToUtf8BomCsv(items);

    private static byte[] ExportPdf(IRptCobroDiarioReadService _, List<RptCobroDiarioRowDto> items)
    {
        var csvBytes = RptCobroDiarioCsvFormatter.ToUtf8BomCsv(items);
        return TabularPdfDocument.FromUtf8BomCsv("Morosidad por gestor", csvBytes);
    }
}
