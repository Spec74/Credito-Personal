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

/// <summary>Cobro diario y cobro diario detalle.</summary>
internal static class CreditoReporteCobroDiarioEndpoints
{
    public static void MapCreditoReporteCobroDiarioEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-cobro-diario",
                async Task<Results<Ok<List<RptCobroDiarioRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    bool? soloMora,
                    IRptCobroDiarioReadService rptCobroDiario,
                    IUsuarioAdminReadService usuarios,
                    ICajaMaestroReadService cajas,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var filtrarSoloMora = soloMora ?? false;
                    var (uid, oid, err) = await CobroDiarioReportAccess.ResolveFiltrosAsync(
                        httpContext,
                        oficinas,
                        usuarioId,
                        oficinaId,
                        requiereGestor: !filtrarSoloMora,
                        ct).ConfigureAwait(false);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptCobroDiario");
                    try
                    {
                        var items = CobroDiarioReportAccess.ApplySoloMora(
                            await rptCobroDiario.ListarAsync(uid, oid, ct).ConfigureAwait(false),
                            filtrarSoloMora);
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCobroDiario");
                        var detail = "No se pudo obtener el reporte de cobro diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCobroDiario")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCobroDiario(UsuarioId, OficinaId). Parámetros obligatorios; deben coincidir con vendix:usuario_id y vendix:oficina_id del JWT. CreditoUser. Equivale a CreditoBL.ReporteCobroDiario.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCobroDiarioRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cobro-diario-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    bool? soloMora,
                    IRptCobroDiarioReadService rptCobroDiario,
                    IUsuarioAdminReadService usuarios,
                    ICajaMaestroReadService cajas,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var filtrarSoloMora = soloMora ?? false;
                    var (uid, oid, err) = await CobroDiarioReportAccess.ResolveFiltrosAsync(
                        httpContext,
                        oficinas,
                        usuarioId,
                        oficinaId,
                        requiereGestor: !filtrarSoloMora,
                        ct).ConfigureAwait(false);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptCobroDiarioCsv");
                    try
                    {
                        var items = CobroDiarioReportAccess.ApplySoloMora(
                            await rptCobroDiario.ListarAsync(uid, oid, ct).ConfigureAwait(false),
                            filtrarSoloMora);
                        var bytes = RptCobroDiarioCsvFormatter.ToUtf8BomCsv(items);
                        var fileName = filtrarSoloMora ? "morosidad-gestor.csv" : "cobro-diario.csv";
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: fileName);
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCobroDiario (CSV)");
                        var detail = "No se pudo generar el CSV de cobro diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCobroDiarioCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-cobro-diario en CSV UTF-8 (BOM). usp_RptCobroDiario sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cobro-diario-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    bool? soloMora,
                    IRptCobroDiarioReadService rptCobroDiario,
                    IUsuarioAdminReadService usuarios,
                    ICajaMaestroReadService cajas,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var filtrarSoloMora = soloMora ?? false;
                    var (uid, oid, err) = await CobroDiarioReportAccess.ResolveFiltrosAsync(
                        httpContext,
                        oficinas,
                        usuarioId,
                        oficinaId,
                        requiereGestor: !filtrarSoloMora,
                        ct).ConfigureAwait(false);
                    if (err is not null)
                    {
                        return err;
                    }

                    var log = loggerFactory.CreateLogger("RptCobroDiarioPdf");
                    try
                    {
                        var items = CobroDiarioReportAccess.ApplySoloMora(
                            await rptCobroDiario.ListarAsync(uid, oid, ct).ConfigureAwait(false),
                            filtrarSoloMora);
                        var pdfContext = await GestorInformePdfContextBuilder
                            .BuildCobroDiarioAsync(items, uid, filtrarSoloMora, usuarios, cajas, ct, oficinas, oid)
                            .ConfigureAwait(false);
                        var bytes = RptCobroDiarioPdfDocument.Build(items, pdfContext, filtrarSoloMora);
                        var fileName = filtrarSoloMora ? "morosidad-gestor.pdf" : "cobro-diario.pdf";
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: fileName);
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCobroDiario (PDF)");
                        var detail = "No se pudo generar el PDF de cobro diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCobroDiarioPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-cobro-diario en PDF tabular (QuestPDF). usp_RptCobroDiario sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cobro-diario-detalle",
                async Task<Results<Ok<List<RptCobroDiarioDetalleRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    IRptCobroDiarioDetalleReadService rptDetalle,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId is null or < 1 || oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId y oficinaId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var jwtUsuarioId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    if (jwtUsuarioId != usuarioId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "usuarioId debe coincidir con el usuario del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RptCobroDiarioDetalle");
                    try
                    {
                        var items = await rptDetalle.ListarAsync(usuarioId.Value, oficinaId.Value, ct).ConfigureAwait(false);
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCobroDiarioDetalle");
                        var detail = "No se pudo obtener el detalle de cobranza del gestor.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCobroDiarioDetalle")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCobroDiarioDetalle(UsuarioId, OficinaId). Parámetros obligatorios; deben coincidir con vendix:usuario_id y vendix:oficina_id del JWT. CreditoUser. Equivale a CreditoBL.ReporteCobranzaGestor.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCobroDiarioDetalleRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cobro-diario-detalle-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    IRptCobroDiarioDetalleReadService rptDetalle,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId is null or < 1 || oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId y oficinaId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var jwtUsuarioId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    if (jwtUsuarioId != usuarioId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "usuarioId debe coincidir con el usuario del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RptCobroDiarioDetalleCsv");
                    try
                    {
                        var items = await rptDetalle.ListarAsync(usuarioId.Value, oficinaId.Value, ct).ConfigureAwait(false);
                        var bytes = RptCobroDiarioDetalleCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "cobro-diario-detalle.csv");
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCobroDiarioDetalle (CSV)");
                        var detail = "No se pudo generar el CSV de detalle de cobranza del gestor.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCobroDiarioDetalleCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-cobro-diario-detalle en CSV UTF-8 (BOM). usp_RptCobroDiarioDetalle sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-cobro-diario-detalle-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? usuarioId,
                    int? oficinaId,
                    IRptCobroDiarioDetalleReadService rptDetalle,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId is null or < 1 || oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId y oficinaId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var jwtUsuarioId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    if (jwtUsuarioId != usuarioId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "usuarioId debe coincidir con el usuario del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("RptCobroDiarioDetallePdf");
                    try
                    {
                        var items = await rptDetalle.ListarAsync(usuarioId.Value, oficinaId.Value, ct).ConfigureAwait(false);
                        var csvBytes = RptCobroDiarioDetalleCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext, oficinaId, usuarioId, cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Cobro diario detalle", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "cobro-diario-detalle.pdf");
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
                        log.LogWarning(ex, "Parámetros fuera de rango");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCobroDiarioDetalle (PDF)");
                        var detail = "No se pudo generar el PDF de detalle de cobranza del gestor.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCobroDiarioDetallePdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-cobro-diario-detalle en PDF tabular (QuestPDF). usp_RptCobroDiarioDetalle sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
