using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

internal static class RptComprobantesCajaChicaEndpoints
{
    public static void MapRptComprobantesCajaChicaEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-comprobantes-caja-chica",
                ListarHandler)
            .WithName("CreditoRptComprobantesCajaChica")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptComprobantesCajaChicaRowDto>>();

        app.MapGet(
                "/api/v1/credito/rpt-comprobantes-caja-chica-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    IRptComprobantesCajaChicaReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (error, range) = ValidateFechas(fechaIni, fechaFin);
                    if (error is not null)
                    {
                        return error;
                    }

                    var log = loggerFactory.CreateLogger("RptComprobantesCajaChicaCsv");
                    try
                    {
                        var items = await read.ListarAsync(range!.Value.Ini, range.Value.Fin, ct).ConfigureAwait(false);
                        var bytes = RptComprobantesCajaChicaCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", "comprobantes-caja-chica.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptComprobantesCajaChicaCsv")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/credito/rpt-comprobantes-caja-chica-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    IRptComprobantesCajaChicaReadService read,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var (error, range) = ValidateFechas(fechaIni, fechaFin);
                    if (error is not null)
                    {
                        return error;
                    }

                    var log = loggerFactory.CreateLogger("RptComprobantesCajaChicaPdf");
                    try
                    {
                        var items = await read.ListarAsync(range!.Value.Ini, range.Value.Fin, ct).ConfigureAwait(false);
                        var csvBytes = RptComprobantesCajaChicaCsvFormatter.ToUtf8BomCsv(items);
                        var pdfCtx = new CredixLegacyReportContext
                        {
                            FechaIni = range!.Value.Ini.ToString("d"),
                            FechaFin = range.Value.Fin.ToString("d"),
                        };
                        var pdfBytes = TabularPdfDocument.FromUtf8BomCsv(
                            CredixLegacyReportKey.ComprobantesCajaChica,
                            csvBytes,
                            pdfCtx);
                        return TypedResults.File(pdfBytes, "application/pdf", "comprobantes-caja-chica.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CreditoRptComprobantesCajaChicaPdf")
            .WithTags("credito-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static async Task<Results<Ok<List<RptComprobantesCajaChicaRowDto>>, ProblemHttpResult>> ListarHandler(
        DateTime? fechaIni,
        DateTime? fechaFin,
        IRptComprobantesCajaChicaReadService read,
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        CancellationToken ct)
    {
        var (error, range) = ValidateFechas(fechaIni, fechaFin);
        if (error is not null)
        {
            return error;
        }

        var log = loggerFactory.CreateLogger("RptComprobantesCajaChica");
        try
        {
            var items = await read.ListarAsync(range!.Value.Ini, range.Value.Fin, ct).ConfigureAwait(false);
            return TypedResults.Ok(items.ToList());
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException or DbException)
        {
            return ReadError(log, env, ex);
        }
    }

    private static (ProblemHttpResult? Error, (DateTime Ini, DateTime Fin)? Range) ValidateFechas(
        DateTime? fechaIni,
        DateTime? fechaFin)
    {
        if (fechaIni is null || fechaFin is null)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "fechaIni y fechaFin son obligatorias."), null);
        }

        if (fechaIni.Value.Date > fechaFin.Value.Date)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "fechaIni no puede ser posterior a fechaFin."), null);
        }

        var yi = fechaIni.Value.Year;
        var yf = fechaFin.Value.Year;
        if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100."), null);
        }

        return (null, (fechaIni.Value, fechaFin.Value));
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Comprobantes caja chica: configuración");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        if (ex is ArgumentOutOfRangeException aor)
        {
            log.LogWarning(aor, "Comprobantes caja chica: parámetros");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "El rango de fechas solicitado no es válido.");
        }

        log.LogError(ex, "Comprobantes caja chica");
        var detail = "No se pudo obtener comprobantes de caja chica.";
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
