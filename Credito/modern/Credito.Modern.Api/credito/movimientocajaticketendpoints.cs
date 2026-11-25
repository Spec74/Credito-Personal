using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Oficinas;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Credito.Modern.Api.Credito;

internal static class MovimientoCajaTicketEndpoints
{
    public static void MapMovimientoCajaTicketEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/movimiento-caja-ticket-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int movimientoCajaId,
                    [FromQuery] int oficinaId,
                    [FromQuery] string? movimientoCajaIds,
                    IMovimientoCajaTicketReadService ticketRead,
                    IMovimientoCajaScopeReadService movimientoScope,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var grupoIds = ParseMovimientoIds(movimientoCajaIds, movimientoCajaId);
                    if (grupoIds.Count == 0 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "movimientoCajaId (o movimientoCajaIds) y oficinaId deben ser >= 1.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    foreach (var id in grupoIds)
                    {
                        var (scopeError, _) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(oficinaId, id, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }
                    }

                    var log = loggerFactory.CreateLogger("MovimientoCajaTicketPdf");
                    try
                    {
                        var data = grupoIds.Count > 1
                            ? await ticketRead.ObtenerGrupoAsync(grupoIds, ct).ConfigureAwait(false)
                            : await ticketRead.ObtenerAsync(grupoIds[0], ct).ConfigureAwait(false);
                        if (data is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Movimiento de caja no existe.");
                        }

                        var bytes = MovimientoCajaTicketPdfDocument.Build(data);
                        var fileId = grupoIds.Count > 1
                            ? string.Join("-", grupoIds)
                            : grupoIds[0].ToString();
                        return TypedResults.File(
                            bytes,
                            "application/pdf",
                            $"ticket-movimiento-caja-{fileId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        if (ex is InvalidOperationException ioe)
                        {
                            log.LogWarning(ioe, "Ticket movimiento caja: configuración");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta");
                        }

                        log.LogError(ex, "Ticket movimiento caja PDF");
                        var detail = "No se pudo generar el ticket.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientoCajaTicketPdf")
            .WithTags("credito-caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .WithSummary(
                "Ticket PDF por movimiento (QuestPDF). Cobro mixto: movimientoCajaIds=7,8 → un comprobante con ambos medios. CreditoUser.");

        app.MapGet(
                "/api/v1/credito/movimiento-caja-chica-ticket-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    [FromQuery] int movimientoCajaChicaId,
                    IMovimientoCajaChicaTicketReadService ticketRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoCajaChicaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "movimientoCajaChicaId debe ser >= 1.");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var jwtUsuarioId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:usuario_id.");
                    }

                    var log = loggerFactory.CreateLogger("MovimientoCajaChicaTicketPdf");
                    try
                    {
                        var data = await ticketRead
                            .ObtenerAsync(movimientoCajaChicaId, jwtUsuarioId, ct)
                            .ConfigureAwait(false);
                        if (data is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Movimiento de caja chica no existe o no pertenece a su sesión abierta.");
                        }

                        var bytes = MovimientoCajaTicketPdfDocument.Build(data);
                        return TypedResults.File(
                            bytes,
                            "application/pdf",
                            $"ticket-movimiento-caja-chica-{movimientoCajaChicaId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        if (ex is InvalidOperationException ioe)
                        {
                            log.LogWarning(ioe, "Ticket movimiento caja chica: configuración");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta");
                        }

                        log.LogError(ex, "Ticket movimiento caja chica PDF");
                        var detail = "No se pudo generar el ticket.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientoCajaChicaTicketPdf")
            .WithTags("credito-caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .WithSummary(
                "Ticket PDF caja chica (QuestPDF). Paridad Reporte/ReporteMovimientoCajaChica sin RDLC. CreditoUser.");

        app.MapGet(
                "/api/v1/credito/movimiento-boveda-ticket-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    [FromQuery] int movimientoBovedaId,
                    IMovimientoBovedaTicketReadService ticketRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoBovedaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "movimientoBovedaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("MovimientoBovedaTicketPdf");
                    try
                    {
                        var data = await ticketRead.ObtenerAsync(movimientoBovedaId, ct).ConfigureAwait(false);
                        if (data is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Movimiento de bóveda no existe.");
                        }

                        var bytes = MovimientoCajaTicketPdfDocument.Build(data);
                        return TypedResults.File(
                            bytes,
                            "application/pdf",
                            $"ticket-movimiento-boveda-{movimientoBovedaId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        if (ex is InvalidOperationException ioe)
                        {
                            log.LogWarning(ioe, "Ticket bóveda: configuración");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta");
                        }

                        log.LogError(ex, "Ticket movimiento bóveda PDF");
                        var detail = "No se pudo generar el ticket.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMovimientoBovedaTicketPdf")
            .WithTags("credito-caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .WithSummary(
                "Ticket PDF movimiento bóveda. Paridad Reporte/ReporteMovimientoBovedaMov sin RDLC. CreditoUser.");
    }

    private static List<int> ParseMovimientoIds(string? movimientoCajaIds, int movimientoCajaId)
    {
        var ids = new List<int>();
        if (!string.IsNullOrWhiteSpace(movimientoCajaIds))
        {
            foreach (var part in movimientoCajaIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var id) && id > 0 && !ids.Contains(id))
                {
                    ids.Add(id);
                }
            }
        }

        if (ids.Count == 0 && movimientoCajaId > 0)
        {
            ids.Add(movimientoCajaId);
        }
        else if (movimientoCajaId > 0 && !ids.Contains(movimientoCajaId))
        {
            ids.Insert(0, movimientoCajaId);
        }

        ids.Sort();
        return ids;
    }
}
