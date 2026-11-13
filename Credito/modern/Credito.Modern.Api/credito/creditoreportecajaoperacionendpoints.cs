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

/// <summary>Caja diario, crédito vencido, notificaciones, movimientos anulados y saldo cartera caja.</summary>
internal static class CreditoReporteCajaOperacionEndpoints
{
    public static void MapCreditoReporteCajaOperacionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-caja-diario",
                async Task<Results<Ok<List<RptCajaDiarioRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCajaDiarioReadService rptCajaDiario,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorias.");
                    }

                    if (fechaIni.Value.Date > fechaFin.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni no puede ser posterior a fechaFin.");
                    }

                    var yi = fechaIni.Value.Year;
                    var yf = fechaFin.Value.Year;
                    if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCajaDiario");
                    try
                    {
                        var items = await rptCajaDiario.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCajaDiario");
                        var detail = "No se pudo obtener el reporte de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCajaDiario")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCajaDiario(UsuarioId, OficinaId, FechaInicio, FechaFin). fechaIni/fechaFin/oficinaId obligatorios; oficinaId = vendix:oficina_id. usuarioId opcional; si se envía debe = vendix:usuario_id. CreditoUser. Equivale a CreditoBL.ReporteCajaDiario / ReporteController.ReporteCajaDiario (sin oficina/usuario null para \"todos\").")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCajaDiarioRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-caja-diario-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCajaDiarioReadService rptCajaDiario,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorias.");
                    }

                    if (fechaIni.Value.Date > fechaFin.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni no puede ser posterior a fechaFin.");
                    }

                    var yi = fechaIni.Value.Year;
                    var yf = fechaFin.Value.Year;
                    if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCajaDiarioCsv");
                    try
                    {
                        var items = await rptCajaDiario.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCajaDiarioCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "caja-diario.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCajaDiario (CSV)");
                        var detail = "No se pudo generar el CSV de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCajaDiarioCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-caja-diario en CSV UTF-8 (BOM). usp_RptCajaDiario sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-caja-diario-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCajaDiarioReadService rptCajaDiario,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorias.");
                    }

                    if (fechaIni.Value.Date > fechaFin.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni no puede ser posterior a fechaFin.");
                    }

                    var yi = fechaIni.Value.Year;
                    var yf = fechaFin.Value.Year;
                    if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCajaDiarioPdf");
                    try
                    {
                        var items = await rptCajaDiario.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCajaDiarioCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolved,
                                usuarioResolved,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Caja diario", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "caja-diario.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCajaDiario (PDF)");
                        var detail = "No se pudo generar el PDF de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCajaDiarioPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-caja-diario en PDF tabular (QuestPDF). usp_RptCajaDiario sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-vencido",
                async Task<Results<Ok<List<RptCreditoVencidoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    string? vencidoMenor60,
                    string? vencidoMayor60,
                    string? vencidoIrrecuperable,
                    IRptCreditoVencidoReadService rptCreditoVencido,
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

                    var log = loggerFactory.CreateLogger("RptCreditoVencido");
                    try
                    {
                        var items = await rptCreditoVencido
                            .ListarAsync(oficinaId.Value, vencidoMenor60, vencidoMayor60, vencidoIrrecuperable, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogWarning(ex, "Indicadores vencido* inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoVencido");
                        var detail = "No se pudo obtener el reporte de créditos vencidos.";
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
            .WithName("CreditoRptCreditoVencido")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditoVencido(VencidoMenor60, VencidoMayor60, VencidoIrrecuperable) con post-filtro por oficina JWT.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoVencidoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-vencido-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    string? vencidoMenor60,
                    string? vencidoMayor60,
                    string? vencidoIrrecuperable,
                    IRptCreditoVencidoReadService rptCreditoVencido,
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

                    var log = loggerFactory.CreateLogger("RptCreditoVencidoCsv");
                    try
                    {
                        var items = await rptCreditoVencido
                            .ListarAsync(oficinaId.Value, vencidoMenor60, vencidoMayor60, vencidoIrrecuperable, ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoVencidoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "credito-vencido.csv");
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
                        log.LogWarning(ex, "Indicadores vencido* inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoVencido (CSV)");
                        var detail = "No se pudo generar el CSV de créditos vencidos.";
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
            .WithName("CreditoRptCreditoVencidoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-vencido en CSV UTF-8 (BOM). usp_RptCreditoVencido sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-vencido-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    string? vencidoMenor60,
                    string? vencidoMayor60,
                    string? vencidoIrrecuperable,
                    IRptCreditoVencidoReadService rptCreditoVencido,
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

                    var log = loggerFactory.CreateLogger("RptCreditoVencidoPdf");
                    try
                    {
                        var items = await rptCreditoVencido
                            .ListarAsync(oficinaId.Value, vencidoMenor60, vencidoMayor60, vencidoIrrecuperable, ct)
                            .ConfigureAwait(false);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaId,
                                fecha: DateTime.Today,
                                titulo: "CRÉDITOS VENCIDOS",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoVencidoFichaPdfDocument.Build(items, pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "credito-vencido.pdf");
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
                        log.LogWarning(ex, "Indicadores vencido* inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoVencido (PDF)");
                        var detail = "No se pudo generar el PDF de créditos vencidos.";
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
            .WithName("CreditoRptCreditoVencidoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-vencido en PDF tabular (QuestPDF). usp_RptCreditoVencido sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/rpt-notificaciones-vencimiento-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int creditoId,
                    IRptEstadoCreditoReadService estadoCredito,
                    IRptClienteReadService cliente,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (creditoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "creditoId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptNotificacionVencimientoPdf");
                    try
                    {
                        var estado = await estadoCredito.ObtenerAsync(creditoId, ct).ConfigureAwait(false);
                        if (estado is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Crédito no existe.");
                        }

                        RptClienteFichaDto? ficha = null;
                        if (estado.Cabecera.PersonaId > 0)
                        {
                            var informeCliente = await cliente
                                .ObtenerAsync(estado.Cabecera.PersonaId, ct)
                                .ConfigureAwait(false);
                            ficha = informeCliente?.Ficha;
                        }

                        var bytes = RptNotificacionVencimientoPdfDocument.Build(estado, ficha);
                        return TypedResults.File(
                            bytes,
                            "application/pdf",
                            fileDownloadName: $"notificacion-vencimiento-{creditoId}.pdf");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al generar notificación de vencimiento PDF");
                        var detail = "No se pudo generar el PDF de notificación de vencimiento.";
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
            .WithName("CreditoRptNotificacionVencimientoPdf")
            .WithSummary(
                "PDF de notificación de vencimiento de un crédito (cliente + cuotas vencidas) para imprimir o enviar. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-caja-anulado",
                async Task<Results<Ok<List<RptMovimientoCajaAnuladoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    IRptMovimientoCajaAnuladoReadService rptMovimientoCajaAnulado,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorias.");
                    }

                    if (fechaIni.Value.Date > fechaFin.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni no puede ser posterior a fechaFin.");
                    }

                    var yi = fechaIni.Value.Year;
                    var yf = fechaFin.Value.Year;
                    if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100.");
                    }

                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoCajaAnulado");
                    try
                    {
                        var items = await rptMovimientoCajaAnulado
                            .ListarAsync(oficinaId.Value, fechaIni.Value, fechaFin.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogWarning(ex, "Rango de fechas inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoCajaAnulado");
                        var detail = "No se pudo obtener el listado de movimientos de caja anulados.";
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
            .WithName("CreditoRptMovimientoCajaAnulado")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptMovimientoCajaAnulado(FechaIni, FechaFin) con post-filtro por oficina JWT.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptMovimientoCajaAnuladoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-caja-anulado-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    IRptMovimientoCajaAnuladoReadService rptMovimientoCajaAnulado,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorias.");
                    }

                    if (fechaIni.Value.Date > fechaFin.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni no puede ser posterior a fechaFin.");
                    }

                    var yi = fechaIni.Value.Year;
                    var yf = fechaFin.Value.Year;
                    if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100.");
                    }

                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoCajaAnuladoCsv");
                    try
                    {
                        var items = await rptMovimientoCajaAnulado
                            .ListarAsync(oficinaId.Value, fechaIni.Value, fechaFin.Value, ct)
                            .ConfigureAwait(false);
                        var bytes = RptMovimientoCajaAnuladoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "movimiento-caja-anulado.csv");
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
                        log.LogWarning(ex, "Rango de fechas inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoCajaAnulado (CSV)");
                        var detail = "No se pudo generar el CSV de movimientos de caja anulados.";
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
            .WithName("CreditoRptMovimientoCajaAnuladoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-movimiento-caja-anulado en CSV UTF-8 (BOM). usp_RptMovimientoCajaAnulado sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-caja-anulado-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    IRptMovimientoCajaAnuladoReadService rptMovimientoCajaAnulado,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorias.");
                    }

                    if (fechaIni.Value.Date > fechaFin.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni no puede ser posterior a fechaFin.");
                    }

                    var yi = fechaIni.Value.Year;
                    var yf = fechaFin.Value.Year;
                    if (yi < 1900 || yi > 2100 || yf < 1900 || yf > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin deben tener año entre 1900 y 2100.");
                    }

                    if (oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoCajaAnuladoPdf");
                    try
                    {
                        var items = await rptMovimientoCajaAnulado
                            .ListarAsync(oficinaId.Value, fechaIni.Value, fechaFin.Value, ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptMovimientoCajaAnuladoCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaId,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Movimiento caja anulado", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "movimiento-caja-anulado.pdf");
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
                        log.LogWarning(ex, "Rango de fechas inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoCajaAnulado (PDF)");
                        var detail = "No se pudo generar el PDF de movimientos de caja anulados.";
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
            .WithName("CreditoRptMovimientoCajaAnuladoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-movimiento-caja-anulado en PDF tabular (QuestPDF). usp_RptMovimientoCajaAnulado sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldo-cartera-caja-diario",
                async Task<Results<Ok<List<RptSaldoCarteraCajaDiarioRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    int? anioIni,
                    int? mesIni,
                    int? anioFin,
                    int? mesFin,
                    IRptSaldoCarteraCajaDiarioReadService rptSaldoCarteraCajaDiario,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (anioIni is null || mesIni is null || anioFin is null || mesFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anioIni, mesIni, anioFin y mesFin son obligatorios.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptSaldoCarteraCajaDiario");
                    try
                    {
                        var items = await rptSaldoCarteraCajaDiario
                            .ListarAsync(
                                usuarioResolved,
                                oficinaResolved,
                                anioIni.Value,
                                mesIni.Value,
                                anioFin.Value,
                                mesFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldoCarteraCajaDiario");
                        var detail = "No se pudo obtener el reporte de saldo cartera por caja diario.";
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
            .WithName("CreditoRptSaldoCarteraCajaDiario")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptSaldoCarteraCajaDiario(UsuarioId, OficinaId, AnioIni, MesIni, AnioFin, MesFin). anioIni/mesIni/anioFin/mesFin y oficinaId obligatorios; oficinaId = vendix:oficina_id. usuarioId opcional; si se envía debe = vendix:usuario_id (como filtro gestor en el MVC). Meses 1â€“12; periodo inicial no posterior al final. CreditoUser. Equivale a CajaDiarioBL.ReporteSaldoCarteraCajaDiario / ReporteController.ReporteSaldoCarteraCajaDiario (sin oficina/usuario null para \"todos\" en oficina). Distinto de GET .../listar-saldo-cartera (usp_ListarSaldoCartera).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptSaldoCarteraCajaDiarioRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldo-cartera-caja-diario-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    int? anioIni,
                    int? mesIni,
                    int? anioFin,
                    int? mesFin,
                    IRptSaldoCarteraCajaDiarioReadService rptSaldoCarteraCajaDiario,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (anioIni is null || mesIni is null || anioFin is null || mesFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anioIni, mesIni, anioFin y mesFin son obligatorios.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptSaldoCarteraCajaDiarioCsv");
                    try
                    {
                        var items = await rptSaldoCarteraCajaDiario
                            .ListarAsync(
                                usuarioResolved,
                                oficinaResolved,
                                anioIni.Value,
                                mesIni.Value,
                                anioFin.Value,
                                mesFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptSaldoCarteraCajaDiarioCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "saldo-cartera-caja-diario.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldoCarteraCajaDiario (CSV)");
                        var detail = "No se pudo generar el CSV de saldo cartera por caja diario.";
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
            .WithName("CreditoRptSaldoCarteraCajaDiarioCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-saldo-cartera-caja-diario en CSV UTF-8 (BOM). usp_RptSaldoCarteraCajaDiario sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldo-cartera-caja-diario-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    int? anioIni,
                    int? mesIni,
                    int? anioFin,
                    int? mesFin,
                    IRptSaldoCarteraCajaDiarioReadService rptSaldoCarteraCajaDiario,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (anioIni is null || mesIni is null || anioFin is null || mesFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anioIni, mesIni, anioFin y mesFin son obligatorios.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptSaldoCarteraCajaDiarioPdf");
                    try
                    {
                        var items = await rptSaldoCarteraCajaDiario
                            .ListarAsync(
                                usuarioResolved,
                                oficinaResolved,
                                anioIni.Value,
                                mesIni.Value,
                                anioFin.Value,
                                mesFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        var activas = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
                        var oficinaNom = activas.Find(o => o.OficinaId == oficinaResolved)?.Denominacion
                            ?? $"Oficina #{oficinaResolved}";
                        var es = CultureInfo.GetCultureInfo("es-PE");
                        var periodoIni = new DateTime(anioIni.Value, mesIni.Value, 1).ToString("MMMM yyyy", es);
                        var periodoFin = new DateTime(anioFin.Value, mesFin.Value, 1).ToString("MMMM yyyy", es);
                        var bytes = RptSaldoCarteraCajaDiarioPdfDocument.Build(
                            items,
                            new RptSaldoCarteraCajaDiarioPdfDocument.Header(
                                oficinaNom,
                                periodoIni,
                                periodoFin));
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "saldo-cartera-caja-diario.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldoCarteraCajaDiario (PDF)");
                        var detail = "No se pudo generar el PDF de saldo cartera por caja diario.";
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
            .WithName("CreditoRptSaldoCarteraCajaDiarioPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-saldo-cartera-caja-diario en PDF Credix (A3 agrupado periodo ini/fin). usp_RptSaldoCarteraCajaDiario. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
