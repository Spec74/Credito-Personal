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

/// <summary>Cartera crédito: estado, morosidad, rentabilidad, aprobación, activos, cierres, morosos pagados.</summary>
internal static class CreditoReporteCreditoCarteraEndpoints
{
    public static void MapCreditoReporteCreditoCarteraEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-credito",
                async Task<Results<Ok<List<RptCreditoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    string? estadoCredito,
                    int? oficinaId,
                    int? gestorId,
                    IRptCreditoReadService rptCredito,
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
                            detail: "fechaIni y fechaFin son obligatorias (fecha, p. ej. ISO 2026-05-01).");
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
                    if (string.IsNullOrWhiteSpace(estadoCredito))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito es obligatorio (p. ej. CRE, DES, PAG; ver combo del MVC).");
                    }

                    if (estadoCredito.Trim().Length > 32)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito admite como máximo 32 caracteres.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, gestorId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCredito");
                    try
                    {
                        var items = await rptCredito.ListarAsync(
                                oficinaResolved,
                                usuarioResolved,
                                estadoCredito,
                                fechaIni.Value,
                                fechaFin.Value,
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCredito");
                        var detail = "No se pudo obtener el reporte de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCredito")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCredito(OficinaId, GestorId, Estado, FechaDesIni, FechaDesFin). fechaIni/fechaFin y estadoCredito obligatorios; oficinaId = vendix:oficina_id. gestorId opcional (TODOS o uno concreto para ADMIN/APROBADOR/PARCIAL). CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    string? estadoCredito,
                    int? oficinaId,
                    int? gestorId,
                    IRptCreditoReadService rptCredito,
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
                            detail: "fechaIni y fechaFin son obligatorias (fecha, p. ej. ISO 2026-05-01).");
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
                    if (string.IsNullOrWhiteSpace(estadoCredito))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito es obligatorio (p. ej. CRE, DES, PAG; ver combo del MVC).");
                    }

                    if (estadoCredito.Trim().Length > 32)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito admite como máximo 32 caracteres.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, gestorId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoCsv");
                    try
                    {
                        var items = await rptCredito.ListarAsync(
                                oficinaResolved,
                                usuarioResolved,
                                estadoCredito,
                                fechaIni.Value,
                                fechaFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "reporte-credito.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCredito (CSV)");
                        var detail = "No se pudo generar el CSV del reporte de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito en CSV UTF-8 (BOM). usp_RptCredito sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    string? estadoCredito,
                    int? oficinaId,
                    int? gestorId,
                    IRptCreditoReadService rptCredito,
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
                            detail: "fechaIni y fechaFin son obligatorias (fecha, p. ej. ISO 2026-05-01).");
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
                    if (string.IsNullOrWhiteSpace(estadoCredito))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito es obligatorio (p. ej. CRE, DES, PAG; ver combo del MVC).");
                    }

                    if (estadoCredito.Trim().Length > 32)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito admite como máximo 32 caracteres.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, gestorId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoPdf");
                    try
                    {
                        var items = await rptCredito.ListarAsync(
                                oficinaResolved,
                                usuarioResolved,
                                estadoCredito,
                                fechaIni.Value,
                                fechaFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditoCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolved,
                                usuarioResolved,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                estado: estadoCredito,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Reporte crédito", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "reporte-credito.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCredito (PDF)");
                        var detail = "No se pudo generar el PDF del reporte de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito en PDF tabular (QuestPDF). usp_RptCredito sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-morosidad",
                async Task<Results<Ok<List<RptCreditoMorosidadRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? hastaFecha,
                    int? diasAtrazoIni,
                    int? diasAtrazoFin,
                    int? oficinaId,
                    IRptCreditoMorosidadReadService morosidad,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (hastaFecha is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "hastaFecha es obligatoria (equivale a pFechaHasta en ReporteController.ReporteCreditoMorosidad).");
                    }

                    var y = hastaFecha.Value.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "hastaFecha debe tener año entre 1900 y 2100.");
                    }

                    if (diasAtrazoIni is null || diasAtrazoFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni y diasAtrazoFin son obligatorios (enteros >= 0; diasAtrazoIni <= diasAtrazoFin).");
                    }

                    if (diasAtrazoIni.Value < 0 || diasAtrazoIni.Value > 500_000
                        || diasAtrazoFin.Value < 0 || diasAtrazoFin.Value > 500_000)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni y diasAtrazoFin deben estar entre 0 y 500000.");
                    }

                    if (diasAtrazoIni.Value > diasAtrazoFin.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni no puede ser mayor que diasAtrazoFin.");
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

                    var log = loggerFactory.CreateLogger("RptCreditoMorosidad");
                    try
                    {
                        var items = await morosidad.ListarAsync(
                                oficinaId.Value,
                                hastaFecha.Value,
                                diasAtrazoIni.Value,
                                diasAtrazoFin.Value,
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoMorosidad");
                        var detail = "No se pudo obtener el reporte de morosidad de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoMorosidad")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditoMorosidad(OficinaId, HastaFecha, DiasAtrazoIni, DiasAtrazoFin). hastaFecha (= pFechaHasta MVC), diasAtrazoIni/Fin y oficinaId obligatorios; oficinaId = vendix:oficina_id. CreditoUser. Equivale a CreditoBL.ReporteCreditoMorosidad (sin oficina null para \"todas\").")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoMorosidadRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-morosidad-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? hastaFecha,
                    int? diasAtrazoIni,
                    int? diasAtrazoFin,
                    int? oficinaId,
                    IRptCreditoMorosidadReadService morosidad,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (hastaFecha is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "hastaFecha es obligatoria (equivale a pFechaHasta en ReporteController.ReporteCreditoMorosidad).");
                    }

                    var y = hastaFecha.Value.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "hastaFecha debe tener año entre 1900 y 2100.");
                    }

                    if (diasAtrazoIni is null || diasAtrazoFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni y diasAtrazoFin son obligatorios (enteros >= 0; diasAtrazoIni <= diasAtrazoFin).");
                    }

                    if (diasAtrazoIni.Value < 0 || diasAtrazoIni.Value > 500_000
                        || diasAtrazoFin.Value < 0 || diasAtrazoFin.Value > 500_000)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni y diasAtrazoFin deben estar entre 0 y 500000.");
                    }

                    if (diasAtrazoIni.Value > diasAtrazoFin.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni no puede ser mayor que diasAtrazoFin.");
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

                    var log = loggerFactory.CreateLogger("RptCreditoMorosidadCsv");
                    try
                    {
                        var items = await morosidad.ListarAsync(
                                oficinaId.Value,
                                hastaFecha.Value,
                                diasAtrazoIni.Value,
                                diasAtrazoFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoMorosidadCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "credito-morosidad.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoMorosidad (CSV)");
                        var detail = "No se pudo generar el CSV de morosidad de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoMorosidadCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-morosidad en CSV UTF-8 (BOM). usp_RptCreditoMorosidad sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-morosidad-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? hastaFecha,
                    int? diasAtrazoIni,
                    int? diasAtrazoFin,
                    int? oficinaId,
                    IRptCreditoMorosidadReadService morosidad,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (hastaFecha is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "hastaFecha es obligatoria (equivale a pFechaHasta en ReporteController.ReporteCreditoMorosidad).");
                    }

                    var y = hastaFecha.Value.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "hastaFecha debe tener año entre 1900 y 2100.");
                    }

                    if (diasAtrazoIni is null || diasAtrazoFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni y diasAtrazoFin son obligatorios (enteros >= 0; diasAtrazoIni <= diasAtrazoFin).");
                    }

                    if (diasAtrazoIni.Value < 0 || diasAtrazoIni.Value > 500_000
                        || diasAtrazoFin.Value < 0 || diasAtrazoFin.Value > 500_000)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni y diasAtrazoFin deben estar entre 0 y 500000.");
                    }

                    if (diasAtrazoIni.Value > diasAtrazoFin.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "diasAtrazoIni no puede ser mayor que diasAtrazoFin.");
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

                    var log = loggerFactory.CreateLogger("RptCreditoMorosidadPdf");
                    try
                    {
                        var items = await morosidad.ListarAsync(
                                oficinaId.Value,
                                hastaFecha.Value,
                                diasAtrazoIni.Value,
                                diasAtrazoFin.Value,
                                ct)
                            .ConfigureAwait(false);
                        var activas = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
                        var oficinaNom = activas.Find(o => o.OficinaId == oficinaId.Value)?.Denominacion
                            ?? $"Oficina #{oficinaId.Value}";
                        var bytes = CredixMorosidadPdfDocument.Build(
                            items,
                            new CredixMorosidadPdfDocument.Header(
                                oficinaNom,
                                CredixReportTokens.FormatDate(hastaFecha.Value),
                                diasAtrazoIni.Value,
                                diasAtrazoFin.Value));
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "credito-morosidad.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoMorosidad (PDF)");
                        var detail = "No se pudo generar el PDF de morosidad de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoMorosidadPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-morosidad en PDF Credix (subfila de contacto, paridad rptCreditoMorosidad). usp_RptCreditoMorosidad. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-rentabilidad",
                async Task<Results<Ok<List<RptCreditoRentabilidadRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    string? estadoCredito,
                    int? oficinaId,
                    IRptCreditoRentabilidadReadService rentabilidadCredito,
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
                            detail: "fechaIni y fechaFin son obligatorias (fecha ISO).");
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

                    if (string.IsNullOrWhiteSpace(estadoCredito))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito es obligatorio (p. ej. DES, PAG; ver combo del MVC).");
                    }

                    if (estadoCredito.Trim().Length > 32)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito admite como máximo 32 caracteres.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoRentabilidad");
                    try
                    {
                        var items = await rentabilidadCredito.ListarAsync(
                                oficinaId.Value,
                                fechaIni.Value,
                                fechaFin.Value,
                                estadoCredito,
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoRentabilidad");
                        var detail = "No se pudo obtener el reporte de rentabilidad de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoRentabilidad")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditoRentabilidad(OficnaId, FechaIni, FechaFin, EstadoCredito). El primer parámetro en SQL se llama OficnaId (typo del EDMX). fechaIni/fechaFin/estadoCredito/oficinaId obligatorios; oficinaId = vendix:oficina_id. CreditoUser. Equivale a CreditoBL.ReporteCreditoRentabilidad (sin pOficinaId null). No replica indTodos del MVC: fechas siempre explícitas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoRentabilidadRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-rentabilidad-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    string? estadoCredito,
                    int? oficinaId,
                    IRptCreditoRentabilidadReadService rentabilidadCredito,
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
                            detail: "fechaIni y fechaFin son obligatorias (fecha ISO).");
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

                    if (string.IsNullOrWhiteSpace(estadoCredito))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito es obligatorio (p. ej. DES, PAG; ver combo del MVC).");
                    }

                    if (estadoCredito.Trim().Length > 32)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito admite como máximo 32 caracteres.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoRentabilidadCsv");
                    try
                    {
                        var items = await rentabilidadCredito.ListarAsync(
                                oficinaId.Value,
                                fechaIni.Value,
                                fechaFin.Value,
                                estadoCredito,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoRentabilidadCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "credito-rentabilidad.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoRentabilidad (CSV)");
                        var detail = "No se pudo generar el CSV de rentabilidad de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoRentabilidadCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-rentabilidad en CSV UTF-8 (BOM). usp_RptCreditoRentabilidad sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-rentabilidad-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    string? estadoCredito,
                    int? oficinaId,
                    IRptCreditoRentabilidadReadService rentabilidadCredito,
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
                            detail: "fechaIni y fechaFin son obligatorias (fecha ISO).");
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

                    if (string.IsNullOrWhiteSpace(estadoCredito))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito es obligatorio (p. ej. DES, PAG; ver combo del MVC).");
                    }

                    if (estadoCredito.Trim().Length > 32)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "estadoCredito admite como máximo 32 caracteres.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoRentabilidadPdf");
                    try
                    {
                        var items = await rentabilidadCredito.ListarAsync(
                                oficinaId.Value,
                                fechaIni.Value,
                                fechaFin.Value,
                                estadoCredito,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditoRentabilidadCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaId,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                estado: estadoCredito,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Rentabilidad crédito", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "credito-rentabilidad.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoRentabilidad (PDF)");
                        var detail = "No se pudo generar el PDF de rentabilidad de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoRentabilidadPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-rentabilidad en PDF tabular (QuestPDF). usp_RptCreditoRentabilidad sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-aprobacion",
                async Task<Results<Ok<List<RptCreditoAprobacionRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaAprobacion,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoAprobacionReadService aprobacion,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (fechaAprobacion is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaAprobacion es obligatoria (equivale a pFecha en ReporteController.ReporteCreditoAprobado).");
                    }

                    var y = fechaAprobacion.Value.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaAprobacion debe tener año entre 1900 y 2100.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoAprobacion");
                    try
                    {
                        var items = await aprobacion.ListarAsync(
                                fechaAprobacion.Value,
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoAprobacion");
                        var detail = "No se pudo obtener el reporte de créditos aprobados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoAprobacion")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditoAprobacion(FechaAprobacion, UsuarioId, OficinaId). fechaAprobacion y oficinaId obligatorios; oficinaId = vendix:oficina_id. usuarioId opcional; si se envía debe = vendix:usuario_id (filtra como gestor en el MVC). CreditoUser. Equivale a CreditoBL.ReporteCreditoAprobacion (sin oficina/usuario null para \"todos\").")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoAprobacionRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-aprobacion-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaAprobacion,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoAprobacionReadService aprobacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaAprobacion is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaAprobacion es obligatoria (equivale a pFecha en ReporteController.ReporteCreditoAprobado).");
                    }

                    var y = fechaAprobacion.Value.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaAprobacion debe tener año entre 1900 y 2100.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoAprobacionCsv");
                    try
                    {
                        var items = await aprobacion.ListarAsync(
                                fechaAprobacion.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoAprobacionCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "credito-aprobacion.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoAprobacion (CSV)");
                        var detail = "No se pudo generar el CSV de créditos aprobados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoAprobacionCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-aprobacion en CSV UTF-8 (BOM). usp_RptCreditoAprobacion sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-aprobacion-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaAprobacion,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoAprobacionReadService aprobacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (fechaAprobacion is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaAprobacion es obligatoria (equivale a pFecha en ReporteController.ReporteCreditoAprobado).");
                    }

                    var y = fechaAprobacion.Value.Year;
                    if (y < 1900 || y > 2100)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaAprobacion debe tener año entre 1900 y 2100.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoAprobacionPdf");
                    try
                    {
                        var items = await aprobacion.ListarAsync(
                                fechaAprobacion.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditoAprobacionCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolved,
                                usuarioResolved,
                                fecha: fechaAprobacion,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Aprobación crédito", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "credito-aprobacion.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditoAprobacion (PDF)");
                        var detail = "No se pudo generar el PDF de créditos aprobados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoAprobacionPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-aprobacion en PDF tabular (QuestPDF). usp_RptCreditoAprobacion sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-activos",
                async Task<Results<Ok<List<RptCreditosActivosRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosActivosReadService creditosActivos,
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

                    var log = loggerFactory.CreateLogger("RptCreditosActivos");
                    try
                    {
                        var items = await creditosActivos.ListarAsync(
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosActivos");
                        var detail = "No se pudo obtener el reporte de créditos activos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosActivos")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditosActivos(FechaIni, FechaFin, UsuarioId, OficinaId). fechaIni/fechaFin/oficinaId obligatorios; oficinaId = vendix:oficina_id. usuarioId opcional; si se envía debe = vendix:usuario_id. CreditoUser. Equivale a CreditoBL.ReporteCreditoActivo / ReporteController.ReporteCreditoActivo (sin nulls \"todos\" en oficina/usuario).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditosActivosRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-activos-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosActivosReadService creditosActivos,
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

                    var log = loggerFactory.CreateLogger("RptCreditosActivosCsv");
                    try
                    {
                        var items = await creditosActivos.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditosActivosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "creditos-activos.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosActivos (CSV)");
                        var detail = "No se pudo generar el CSV de créditos activos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosActivosCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-creditos-activos en CSV UTF-8 (BOM). usp_RptCreditosActivos sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-activos-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosActivosReadService creditosActivos,
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

                    var log = loggerFactory.CreateLogger("RptCreditosActivosPdf");
                    try
                    {
                        var items = await creditosActivos.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditosActivosCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolved,
                                usuarioResolved,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Créditos activos", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "creditos-activos.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosActivos (PDF)");
                        var detail = "No se pudo generar el PDF de créditos activos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosActivosPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-creditos-activos en PDF tabular (QuestPDF). usp_RptCreditosActivos sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-cierres",
                async Task<Results<Ok<List<RptCreditosCierresRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosCierresReadService cierres,
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

                    var log = loggerFactory.CreateLogger("RptCreditosCierres");
                    try
                    {
                        var items = await cierres.ListarAsync(
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosCierres");
                        var detail = "No se pudo obtener el reporte de cierres de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosCierres")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditosCierres(FechaIni, FechaFin, UsuarioId, OficinaId). Misma forma de auth que rpt-creditos-activos. CreditoUser. Equivale a CreditoBL.ReporteCreditoCierre / ReporteController.ReporteCreditoCierre.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditosCierresRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-cierres-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosCierresReadService cierres,
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

                    var log = loggerFactory.CreateLogger("RptCreditosCierresCsv");
                    try
                    {
                        var items = await cierres.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditosCierresCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "creditos-cierres.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosCierres (CSV)");
                        var detail = "No se pudo generar el CSV de cierres de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosCierresCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-creditos-cierres en CSV UTF-8 (BOM). usp_RptCreditosCierres sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-cierres-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosCierresReadService cierres,
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

                    var log = loggerFactory.CreateLogger("RptCreditosCierresPdf");
                    try
                    {
                        var items = await cierres.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditosCierresCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolved,
                                usuarioResolved,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Créditos cierres", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "creditos-cierres.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosCierres (PDF)");
                        var detail = "No se pudo generar el PDF de cierres de créditos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosCierresPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-creditos-cierres en PDF tabular (QuestPDF). usp_RptCreditosCierres sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-morosos-pagados",
                async Task<Results<Ok<List<RptCreditosMorososPagadosRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosMorososPagadosReadService morososPagados,
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

                    var log = loggerFactory.CreateLogger("RptCreditosMorososPagados");
                    try
                    {
                        var items = await morososPagados.ListarAsync(
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosMorososPagados");
                        var detail = "No se pudo obtener el reporte de créditos morosos pagados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosMorososPagados")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptCreditosMorososPagados(UsuarioId, OficinaId, FechaInicio, FechaFin). Orden de parámetros según EDMX/EF. fechaIni/fechaFin/oficinaId obligatorios; oficinaId = vendix:oficina_id. usuarioId opcional; si se envía debe = vendix:usuario_id. CreditoUser. Equivale a CreditoBL.ReporteCreditoMorosoPagado / ReporteController.ReporteCreditoMorosoPagado.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditosMorososPagadosRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-morosos-pagados-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosMorososPagadosReadService morososPagados,
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

                    var log = loggerFactory.CreateLogger("RptCreditosMorososPagadosCsv");
                    try
                    {
                        var items = await morososPagados.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditosMorososPagadosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "creditos-morosos-pagados.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosMorososPagados (CSV)");
                        var detail = "No se pudo generar el CSV de créditos morosos pagados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosMorososPagadosCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-creditos-morosos-pagados en CSV UTF-8 (BOM). usp_RptCreditosMorososPagados sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-creditos-morosos-pagados-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditosMorososPagadosReadService morososPagados,
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

                    var log = loggerFactory.CreateLogger("RptCreditosMorososPagadosPdf");
                    try
                    {
                        var items = await morososPagados.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                usuarioResolved,
                                oficinaResolved,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditosMorososPagadosCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolved,
                                usuarioResolved,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Morosos pagados", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "creditos-morosos-pagados.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptCreditosMorososPagados (PDF)");
                        var detail = "No se pudo generar el PDF de créditos morosos pagados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditosMorososPagadosPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-creditos-morosos-pagados en PDF tabular (QuestPDF). usp_RptCreditosMorososPagados sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
