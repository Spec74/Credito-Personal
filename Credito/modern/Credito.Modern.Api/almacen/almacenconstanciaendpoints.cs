using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Reportes;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Reportes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Credito.Modern.Api.Almacen;

internal static class AlmacenConstanciaEndpoints
{
    public static void MapAlmacenConstanciaEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/almacen/rpt-constancia-almacen",
                ObtenerHandler)
            .WithName("AlmacenRptConstanciaAlmacen")
            .WithTags("almacen-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RptConstanciaAlmacenDto>();

        app.MapGet(
                "/api/v1/almacen/rpt-constancia-almacen-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int movimientoId,
                    [FromQuery] int oficinaId,
                    IRptConstanciaAlmacenReadService constancia,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var guard = await ValidateAsync(httpContext, movimientoId, oficinaId, movimientoOficina, ct)
                        .ConfigureAwait(false);
                    if (guard.Error is not null)
                    {
                        return guard.Error;
                    }

                    var log = loggerFactory.CreateLogger("RptConstanciaAlmacenCsv");
                    try
                    {
                        var dto = await constancia.ObtenerAsync(movimientoId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return NotFoundMovimiento();
                        }

                        var bytes = RptConstanciaAlmacenCsvFormatter.ToUtf8BomCsv(dto);
                        return TypedResults.File(
                            bytes,
                            "text/csv; charset=utf-8",
                            $"constancia-almacen-{movimientoId}.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("AlmacenRptConstanciaAlmacenCsv")
            .WithTags("almacen-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        app.MapGet(
                "/api/v1/almacen/rpt-constancia-almacen-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int movimientoId,
                    [FromQuery] int oficinaId,
                    IRptConstanciaAlmacenReadService constancia,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var guard = await ValidateAsync(httpContext, movimientoId, oficinaId, movimientoOficina, ct)
                        .ConfigureAwait(false);
                    if (guard.Error is not null)
                    {
                        return guard.Error;
                    }

                    var log = loggerFactory.CreateLogger("RptConstanciaAlmacenPdf");
                    try
                    {
                        var dto = await constancia.ObtenerAsync(movimientoId, ct).ConfigureAwait(false);
                        if (dto is null)
                        {
                            return NotFoundMovimiento();
                        }

                        var csvBytes = RptConstanciaAlmacenCsvFormatter.ToUtf8BomCsv(dto);
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(httpContext, oficinaId, cancellationToken: ct)
                            .ConfigureAwait(false);
                        var pdfBytes = TabularPdfDocument.FromUtf8BomCsv(
                            $"Constancia almacén {movimientoId}",
                            csvBytes,
                            context: pdfContext with
                            {
                                Titulo = "CONSTANCIA DE ALMACÉN",
                                Referencia = $"Movimiento #{movimientoId}",
                            });
                        return TypedResults.File(
                            pdfBytes,
                            "application/pdf",
                            $"constancia-almacen-{movimientoId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("AlmacenRptConstanciaAlmacenPdf")
            .WithTags("almacen-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static async Task<Results<Ok<RptConstanciaAlmacenDto>, ProblemHttpResult>> ObtenerHandler(
        HttpContext httpContext,
        [FromQuery] int movimientoId,
        [FromQuery] int oficinaId,
        IRptConstanciaAlmacenReadService constancia,
        IMovimientoOficinaReadService movimientoOficina,
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        CancellationToken ct)
    {
        var guard = await ValidateAsync(httpContext, movimientoId, oficinaId, movimientoOficina, ct)
            .ConfigureAwait(false);
        if (guard.Error is not null)
        {
            return guard.Error;
        }

        var log = loggerFactory.CreateLogger("RptConstanciaAlmacen");
        try
        {
            var dto = await constancia.ObtenerAsync(movimientoId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                return NotFoundMovimiento();
            }

            return TypedResults.Ok(dto);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
        {
            return ReadError(log, env, ex);
        }
    }

    private static async Task<(ProblemHttpResult? Error, int MovimientoId)> ValidateAsync(
        HttpContext httpContext,
        int movimientoId,
        int oficinaId,
        IMovimientoOficinaReadService movimientoOficina,
        CancellationToken ct)
    {
        if (movimientoId < 1 || oficinaId < 1)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "movimientoId y oficinaId deben ser >= 1."), movimientoId);
        }

        var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
        if (oficinaError is not null)
        {
            return (oficinaError, movimientoId);
        }

        var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(movimientoId, ct).ConfigureAwait(false);
        if (movOid is null)
        {
            return (NotFoundMovimiento(), movimientoId);
        }

        if (movOid.Value != oficinaId)
        {
            return (TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "El movimiento no pertenece a la oficina del token JWT."), movimientoId);
        }

        return (null, movimientoId);
    }

    private static ProblemHttpResult NotFoundMovimiento() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "No encontrado",
            detail: "No existe el movimiento de almacén indicado.");

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Configuración");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Constancia almacén");
        var detail = "No se pudo obtener la constancia de almacén.";
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
