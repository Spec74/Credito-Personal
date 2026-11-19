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

/// <summary>Rpt rentabilidad y aprobación.</summary>
internal static partial class CreditoReporteCreditoCarteraEndpoints
{
    private static void MapRptRentabilidadYAprobacion(WebApplication app)
    {
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



    }
}