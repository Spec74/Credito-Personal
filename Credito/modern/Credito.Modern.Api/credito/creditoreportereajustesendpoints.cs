using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Api.Reportes;
using Credito.Modern.Application.Almacenes;
using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTareas;
using Credito.Modern.Application.CreditoTasas;
using Credito.Modern.Application.Dashboard;
using Credito.Modern.Application.Documentos;
using Credito.Modern.Application.Integraciones;
using Credito.Modern.Application.Inventario;
using Credito.Modern.Application.ListaPrecios;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Ventas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

/// <summary>Movimiento crédito y cajas asignadas.</summary>
internal static class CreditoReporteReajustesEndpoints
{
    public static void MapCreditoReporteReajustesEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-movimiento-credito",
                async Task<Results<Ok<List<RptMovimientoCreditoRowDto>>, ProblemHttpResult>> (
                    int? creditoId,
                    IRptMovimientoCreditoReadService rptMovimiento,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoCredito");
                    try
                    {
                        var items = await rptMovimiento.ListarPorCreditoAsync(creditoId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "creditoId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoCredito");
                        var detail = "No se pudo obtener el reporte de movimientos del crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptMovimientoCredito")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptMovimientoCredito(CreditoId). creditoId obligatorio (>=1). CreditoUser. Equivale a CreditoBL.ReporteCreditoMovimiento.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptMovimientoCreditoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-credito-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int? creditoId,
                    IRptMovimientoCreditoReadService rptMovimiento,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoCreditoCsv");
                    try
                    {
                        var items = await rptMovimiento.ListarPorCreditoAsync(creditoId.Value, ct).ConfigureAwait(false);
                        var bytes = RptMovimientoCreditoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "movimiento-credito.csv");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "creditoId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoCredito (CSV)");
                        var detail = "No se pudo generar el CSV de movimientos del crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptMovimientoCreditoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-movimiento-credito en CSV UTF-8 (BOM). CREDITO.usp_RptMovimientoCredito sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-credito-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int? creditoId,
                    IRptMovimientoCreditoReadService rptMovimiento,
                    IRptEstadoCreditoReadService estadoCredito,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoCreditoPdf");
                    try
                    {
                        var items = await rptMovimiento.ListarPorCreditoAsync(creditoId.Value, ct).ConfigureAwait(false);
                        var informe = await estadoCredito.ObtenerAsync(creditoId.Value, ct).ConfigureAwait(false);
                        var bytes = RptMovimientoCreditoFichaPdfDocument.Build(
                            informe?.Cabecera,
                            creditoId.Value,
                            items);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "movimiento-credito.pdf");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "creditoId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoCredito (PDF)");
                        var detail = "No se pudo generar el PDF de movimientos del crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptMovimientoCreditoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-movimiento-credito en PDF ficha profesional (QuestPDF). CREDITO.usp_RptMovimientoCredito sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cajas-asignadas",
                async Task<Results<Ok<List<RptCajasAsignadasRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IRptCajasAsignadasReadService rptCajas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RptCajasAsignadas");
                    try
                    {
                        var items = await rptCajas.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCajasAsignadas");
                        var detail = "No se pudo obtener el reporte de cajas asignadas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCajasAsignadas")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCajasAsignadas(OficinaId). oficinaId obligatorio y debe coincidir con vendix:oficina_id del JWT. CreditoUser. Equivale a CajaBL.LstCajaDiarioOficina.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCajasAsignadasRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cajas-asignadas-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IRptCajasAsignadasReadService rptCajas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RptCajasAsignadasCsv");
                    try
                    {
                        var items = await rptCajas.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        var bytes = RptCajasAsignadasCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "cajas-asignadas.csv");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCajasAsignadas (CSV)");
                        var detail = "No se pudo generar el CSV de cajas asignadas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCajasAsignadasCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-cajas-asignadas en CSV UTF-8 (BOM). usp_RptCajasAsignadas sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cajas-asignadas-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IRptCajasAsignadasReadService rptCajas,
                    IRptSaldosCajaResumenTipoCuentaReadService resumenTipo,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RptCajasAsignadasPdf");
                    try
                    {
                        var items = await rptCajas.ListarPorOficinaAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        var resumenOficina = await resumenTipo
                            .ObtenerPrimerTextoAsync(oficinaId.Value, ct)
                            .ConfigureAwait(false);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext, oficinaId, cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = RptCajasAsignadasPdfDocument.Build(items, pdfContext, resumenOficina);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "cajas-asignadas.pdf");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "oficinaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCajasAsignadas (PDF)");
                        var detail = "No se pudo generar el PDF de cajas asignadas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCajasAsignadasPdf")
            .WithSummary(
                "Export lectura: paridad rptCajasAsignadas.rdlc (QuestPDF). usp_RptCajasAsignadas + usp_RptSaldosCajaResumenTipoCuenta. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
