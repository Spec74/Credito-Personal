using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
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
                    IMovimientoCajaTicketReadService ticketRead,
                    IMovimientoCajaScopeReadService movimientoScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoCajaId < 1 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "movimientoCajaId y oficinaId deben ser >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId)
                        || jwtOficinaId != oficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con vendix:oficina_id del JWT.");
                    }

                    var (scopeError, scope) = await CajaCreditoWriteGuards
                        .ValidateMovimientoOficinaAsync(oficinaId, movimientoCajaId, movimientoScope, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    _ = scope;

                    var log = loggerFactory.CreateLogger("MovimientoCajaTicketPdf");
                    try
                    {
                        var data = await ticketRead.ObtenerAsync(movimientoCajaId, ct).ConfigureAwait(false);
                        if (data is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Movimiento de caja no existe.");
                        }

                        var bytes = MovimientoCajaTicketPdfDocument.Build(data);
                        return TypedResults.File(
                            bytes,
                            "application/pdf",
                            $"ticket-movimiento-caja-{movimientoCajaId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        if (ex is InvalidOperationException ioe)
                        {
                            log.LogWarning(ioe, "Ticket movimiento caja: configuración");
                            return TypedResults.Problem(
                                detail: ioe.Message,
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
                "Ticket PDF por movimiento (QuestPDF). Paridad Reporte/ReporteMovimientoCaja sin RDLC. CreditoUser.");

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
                                detail: ioe.Message,
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
                                detail: ioe.Message,
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
}
