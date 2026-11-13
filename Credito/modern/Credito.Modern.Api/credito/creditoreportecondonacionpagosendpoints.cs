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

/// <summary>Pagos no verificados, crédito observado y condonado.</summary>
internal static class CreditoReporteCondonacionPagosEndpoints
{
    public static void MapCreditoReporteCondonacionPagosEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/pagos-no-verificados-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IPagosNoVerificadosReadService pagosNoVerificados,
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

                    var log = loggerFactory.CreateLogger("PagosNoVerificadosCsv");
                    try
                    {
                        var items = await pagosNoVerificados.ListarAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        var bytes = PagosNoVerificadosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "pagos-no-verificados.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagosNoVerificados (CSV)");
                        var detail = "No se pudo generar el CSV de pagos no verificados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagosNoVerificadosCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET pagos-no-verificados en CSV UTF-8 (BOM). usp_PagosNoVerificados sin motor RDLC; oficinaId solo JWT. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/pagos-no-verificados-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IPagosNoVerificadosReadService pagosNoVerificados,
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

                    var log = loggerFactory.CreateLogger("PagosNoVerificadosPdf");
                    try
                    {
                        var items = await pagosNoVerificados.ListarAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        var csvBytes = PagosNoVerificadosCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext, oficinaId, cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Pagos no verificados", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "pagos-no-verificados.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagosNoVerificados (PDF)");
                        var detail = "No se pudo generar el PDF de pagos no verificados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagosNoVerificadosPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET pagos-no-verificados en PDF tabular (QuestPDF). usp_PagosNoVerificados sin motor RDLC; oficinaId solo JWT. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-observado",
                async Task<Results<Ok<List<RptCreditoObservadoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoObservadoReadService rptCreditoObservado,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    var (oficinaResolvedObs, usuarioResolvedObs, accessObsErr) =
                        await GestorInformeReportAccess.ResolveOptionalOficinaAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessObsErr is not null)
                    {
                        return accessObsErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoObservado");
                    try
                    {
                        var items = await rptCreditoObservado
                            .ListarAsync(oficinaResolvedObs, usuarioResolvedObs, ct)
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
                        log.LogError(ex, "Error al consultar créditos observados");
                        var detail = "No se pudo obtener el reporte de créditos observados.";
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
            .WithName("CreditoRptCreditoObservado")
            .WithSummary(
                "Solo lectura: consulta equivalente a CreditoBL.ReporteCreditoObservado (CREDITO.Credito con Observacion y estado PEN/DES; **no** usa usp_*). oficinaId obligatorio = vendix:oficina_id. usuarioId opcional = vendix:usuario_id (filtra por UsuarioRegId). CreditoUser. Datos JSON para sustituir rptCreditoObservado.rdlc en integraciones.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoObservadoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-observado-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoObservadoReadService rptCreditoObservado,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    var (oficinaResolvedObsCsv, usuarioResolvedObsCsv, accessObsCsvErr) =
                        await GestorInformeReportAccess.ResolveOptionalOficinaAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessObsCsvErr is not null)
                    {
                        return accessObsCsvErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoObservadoCsv");
                    try
                    {
                        var items = await rptCreditoObservado
                            .ListarAsync(oficinaResolvedObsCsv, usuarioResolvedObsCsv, ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoObservadoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "credito-observado.csv");
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
                        log.LogError(ex, "Error al consultar créditos observados (CSV)");
                        var detail = "No se pudo generar el CSV de créditos observados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoObservadoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-observado en CSV UTF-8 (BOM). CreditoBL.ReporteCreditoObservado sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-observado-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoObservadoReadService rptCreditoObservado,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveOptionalOficinaAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoObservadoPdf");
                    try
                    {
                        var items = await rptCreditoObservado
                            .ListarAsync(oficinaResolved, usuarioResolved, ct)
                            .ConfigureAwait(false);
                        var pdfContext = await GestorInformePdfContextBuilder
                            .BuildGestorOficinaAsync(oficinaResolved, usuarioResolved, usuarios, oficinas, ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoObservadoFichaPdfDocument.Build(items, pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "credito-observado.pdf");
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
                        log.LogError(ex, "Error al consultar créditos observados (PDF)");
                        var detail = "No se pudo generar el PDF de créditos observados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoObservadoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-observado en PDF ficha profesional (QuestPDF). Paridad CreditoBL.ReporteCreditoObservado. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-condonado",
                async Task<Results<Ok<List<RptCreditoCondonadoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoCondonadoReadService rptCreditoCondonado,
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
                            detail: "fechaIni y fechaFin son obligatorios.");
                    }
                    if (fechaFin.Value.Date < fechaIni.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaFin no puede ser anterior a fechaIni.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoCondonado");
                    try
                    {
                        var items = await rptCreditoCondonado
                            .ListarAsync(fechaIni.Value, fechaFin.Value, oficinaResolved, usuarioResolved, ct)
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
                        log.LogError(ex, "Error al consultar créditos condonados");
                        var detail = "No se pudo obtener el reporte de créditos condonados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoCondonado")
            .WithSummary(
                "Solo lectura: consulta equivalente a CreditoBL.ReporteCreditoCondonado (PAG + IndCondonacion; FechaMod en rango). fechaIni/fechaFin/oficinaId obligatorios; oficinaId = vendix:oficina_id. usuarioId opcional = vendix:usuario_id. CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoCondonadoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-condonado-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoCondonadoReadService rptCreditoCondonado,
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
                            detail: "fechaIni y fechaFin son obligatorios.");
                    }
                    if (fechaFin.Value.Date < fechaIni.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaFin no puede ser anterior a fechaIni.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoCondonadoCsv");
                    try
                    {
                        var items = await rptCreditoCondonado
                            .ListarAsync(fechaIni.Value, fechaFin.Value, oficinaResolved, usuarioResolved, ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoCondonadoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "credito-condonado.csv");
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
                        log.LogError(ex, "Error al consultar créditos condonados (CSV)");
                        var detail = "No se pudo generar el CSV de créditos condonados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoCondonadoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-condonado en CSV UTF-8 (BOM). CreditoBL.ReporteCreditoCondonado sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-credito-condonado-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptCreditoCondonadoReadService rptCreditoCondonado,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (fechaIni is null || fechaFin is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaIni y fechaFin son obligatorios.");
                    }
                    if (fechaFin.Value.Date < fechaIni.Value.Date)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "fechaFin no puede ser anterior a fechaIni.");
                    }

                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptCreditoCondonadoPdf");
                    try
                    {
                        var items = await rptCreditoCondonado
                            .ListarAsync(fechaIni.Value, fechaFin.Value, oficinaResolved, usuarioResolved, ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditoCondonadoCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await GestorInformePdfContextBuilder
                            .BuildGestorOficinaAsync(oficinaResolved, usuarioResolved, usuarios, oficinas, ct)
                            .ConfigureAwait(false);
                        pdfContext = pdfContext with
                        {
                            FechaIni = CredixReportTokens.FormatDate(fechaIni.Value),
                            FechaFin = CredixReportTokens.FormatDate(fechaFin.Value),
                        };
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            "Créditos condonados",
                            csvBytes,
                            context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "credito-condonado.pdf");
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
                        log.LogError(ex, "Error al consultar créditos condonados (PDF)");
                        var detail = "No se pudo generar el PDF de créditos condonados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptCreditoCondonadoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-credito-condonado en PDF tabular (QuestPDF). Sin layout RDLC legacy. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
