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

/// <summary>Clientes bloqueados, tope crédito, inactivos y nuevos del mes.</summary>
internal static class CreditoReporteClientesEndpoints
{
    public static void MapCreditoReporteClientesEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-clientes-bloqueados",
                async Task<Results<Ok<List<RptClientesBloqueadosRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesBloqueadosReadService rptClientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    var (oficinaResolved, usuarioResolved, accessErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErr is not null)
                    {
                        return accessErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesBloqueados");
                    try
                    {
                        var items = await rptClientes.ListarAsync(oficinaResolved, usuarioResolved, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesBloqueados");
                        var detail = "No se pudo obtener el reporte de clientes bloqueados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesBloqueados")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptClientesBloqueados(OficinaId, UsuarioId). oficinaId = vendix:oficina_id. usuarioId opcional (TODOS solo ADMIN/APROBADOR/PARCIAL); cajero sin usuarioId filtra por su JWT. CreditoUser. Equivale a CreditoBL.ReporteClientesBloqueados.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptClientesBloqueadosRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-bloqueados-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesBloqueadosReadService rptClientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    var (oficinaResolvedCsv, usuarioResolvedCsv, accessErrCsv) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErrCsv is not null)
                    {
                        return accessErrCsv;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesBloqueadosCsv");
                    try
                    {
                        var items = await rptClientes.ListarAsync(oficinaResolvedCsv, usuarioResolvedCsv, ct).ConfigureAwait(false);
                        var bytes = RptClientesBloqueadosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "clientes-bloqueados.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesBloqueados (CSV)");
                        var detail = "No se pudo generar el CSV de clientes bloqueados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesBloqueadosCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-bloqueados en CSV UTF-8 (BOM). usp_RptClientesBloqueados sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-bloqueados-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesBloqueadosReadService rptClientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    var (oficinaResolvedCsv, usuarioResolvedCsv, accessErrCsv) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErrCsv is not null)
                    {
                        return accessErrCsv;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesBloqueadosPdf");
                    try
                    {
                        var items = await rptClientes.ListarAsync(oficinaResolvedCsv, usuarioResolvedCsv, ct).ConfigureAwait(false);
                        var csvBytes = RptClientesBloqueadosCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolvedCsv,
                                usuarioResolvedCsv,
                                fecha: DateTime.Today,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Clientes bloqueados", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "clientes-bloqueados.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesBloqueados (PDF)");
                        var detail = "No se pudo generar el PDF de clientes bloqueados.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesBloqueadosPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-bloqueados en PDF tabular (QuestPDF). usp_RptClientesBloqueados sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-tope-credito",
                async Task<Results<Ok<List<RptClientesTopeCreditoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesTopeCreditoReadService rptTope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    var (oficinaResolvedTope, usuarioResolvedTope, accessErrTope) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErrTope is not null)
                    {
                        return accessErrTope;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesTopeCredito");
                    try
                    {
                        var items = await rptTope.ListarAsync(oficinaResolvedTope, usuarioResolvedTope, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesTopeCredito");
                        var detail = "No se pudo obtener el reporte de clientes con tope de crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesTopeCredito")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptClientesTopeCredito(OficinaId, UsuarioId). oficinaId = vendix:oficina_id. usuarioId opcional (TODOS solo roles elevados). CreditoUser. Equivale a CreditoBL.ReporteClientesTopeCredito.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptClientesTopeCreditoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-tope-credito-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesTopeCreditoReadService rptTope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    var (oficinaResolvedTopeCsv, usuarioResolvedTopeCsv, accessErrTopeCsv) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErrTopeCsv is not null)
                    {
                        return accessErrTopeCsv;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesTopeCreditoCsv");
                    try
                    {
                        var items = await rptTope.ListarAsync(oficinaResolvedTopeCsv, usuarioResolvedTopeCsv, ct).ConfigureAwait(false);
                        var bytes = RptClientesTopeCreditoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "clientes-tope-credito.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesTopeCredito (CSV)");
                        var detail = "No se pudo generar el CSV de clientes con tope de crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesTopeCreditoCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-tope-credito en CSV UTF-8 (BOM). usp_RptClientesTopeCredito sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-tope-credito-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesTopeCreditoReadService rptTope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    var (oficinaResolvedTopeCsv, usuarioResolvedTopeCsv, accessErrTopeCsv) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessErrTopeCsv is not null)
                    {
                        return accessErrTopeCsv;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesTopeCreditoPdf");
                    try
                    {
                        var items = await rptTope.ListarAsync(oficinaResolvedTopeCsv, usuarioResolvedTopeCsv, ct).ConfigureAwait(false);
                        var csvBytes = RptClientesTopeCreditoCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolvedTopeCsv,
                                usuarioResolvedTopeCsv,
                                fecha: DateTime.Today,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Clientes tope crédito", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "clientes-tope-credito.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesTopeCredito (PDF)");
                        var detail = "No se pudo generar el PDF de clientes con tope de crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesTopeCreditoPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-tope-credito en PDF tabular (QuestPDF). usp_RptClientesTopeCredito sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/rpt-clientes-inactivos",
                async Task<Results<Ok<List<RptClientesInactivosRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesInactivosReadService clientesInactivos,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if ((fechaIni is null) != (fechaFin is null))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Indique fechaIni y fechaFin juntas, o ninguna (paridad ReporteClientesInactivos del MVC).");
                    }

                    if (fechaIni is not null && fechaFin is not null)
                    {
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
                    }

                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    var (oficinaResolvedInactivos, usuarioResolvedInactivos, accessInactivosErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessInactivosErr is not null)
                    {
                        return accessInactivosErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesInactivos");
                    try
                    {
                        var items = await clientesInactivos.ListarAsync(
                                fechaIni,
                                fechaFin,
                                usuarioResolvedInactivos,
                                oficinaResolvedInactivos,
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesInactivos");
                        var detail = "No se pudo obtener el reporte de clientes inactivos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesInactivos")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptClientesInactivos(UsuarioId, OficinaId, FechaInicio, FechaFin). oficinaId = vendix:oficina_id. fechaIni/fechaFin opcionales (juntas o ninguna; paridad ReporteClientesInactivos MVC). usuarioId opcional (TODOS solo roles elevados). CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptClientesInactivosRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-inactivos-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesInactivosReadService clientesInactivos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if ((fechaIni is null) != (fechaFin is null))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Indique fechaIni y fechaFin juntas, o ninguna (paridad ReporteClientesInactivos del MVC).");
                    }

                    if (fechaIni is not null && fechaFin is not null)
                    {
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
                    }

                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    var (oficinaResolvedInactivosCsv, usuarioResolvedInactivosCsv, accessInactivosCsvErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessInactivosCsvErr is not null)
                    {
                        return accessInactivosCsvErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesInactivosCsv");
                    try
                    {
                        var items = await clientesInactivos.ListarAsync(
                                fechaIni,
                                fechaFin,
                                usuarioResolvedInactivosCsv,
                                oficinaResolvedInactivosCsv,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptClientesInactivosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "clientes-inactivos.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesInactivos (CSV)");
                        var detail = "No se pudo generar el CSV de clientes inactivos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesInactivosCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-inactivos en CSV UTF-8 (BOM). usp_RptClientesInactivos sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-inactivos-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesInactivosReadService clientesInactivos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if ((fechaIni is null) != (fechaFin is null))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Indique fechaIni y fechaFin juntas, o ninguna (paridad ReporteClientesInactivos del MVC).");
                    }

                    if (fechaIni is not null && fechaFin is not null)
                    {
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
                    }

                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    var (oficinaResolvedInactivosCsv, usuarioResolvedInactivosCsv, accessInactivosCsvErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessInactivosCsvErr is not null)
                    {
                        return accessInactivosCsvErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesInactivosPdf");
                    try
                    {
                        var items = await clientesInactivos.ListarAsync(
                                fechaIni,
                                fechaFin,
                                usuarioResolvedInactivosCsv,
                                oficinaResolvedInactivosCsv,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptClientesInactivosCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaResolvedInactivosCsv,
                                usuarioResolvedInactivosCsv,
                                fechaIni: fechaIni,
                                fechaFin: fechaFin,
                                titulo: "CLIENTES INACTIVOS",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            CredixLegacyReportKey.ClientesInactivos,
                            csvBytes,
                            pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "clientes-inactivos.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptClientesInactivos (PDF)");
                        var detail = "No se pudo generar el PDF de clientes inactivos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesInactivosPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-inactivos en PDF tabular (QuestPDF). usp_RptClientesInactivos sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/rpt-clientes-nuevos-mes",
                async Task<Results<Ok<List<RptCreditoObservadoRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesNuevosMesReadService clientesNuevosMes,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    IDatabaseTimeProvider databaseTime,
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

                    DateTime inicio;
                    DateTime fin;
                    if (fechaIni is null || fechaFin is null)
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor para el periodo por defecto.");
                        }

                        var fecha = serverTime.Value.Date;
                        inicio = fecha.AddDays(-fecha.Day + 1);
                        fin = inicio.AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        if (fechaFin.Value.Date < fechaIni.Value.Date)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Parámetros inválidos",
                                detail: "fechaFin no puede ser anterior a fechaIni.");
                        }

                        inicio = fechaIni.Value.Date;
                        fin = fechaFin.Value.Date;
                    }

                    var (oficinaResolvedNuevos, usuarioResolvedNuevos, accessNuevosErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessNuevosErr is not null)
                    {
                        return accessNuevosErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesNuevosMes");
                    try
                    {
                        var items = await clientesNuevosMes
                            .ListarAsync(inicio, fin, oficinaResolvedNuevos, usuarioResolvedNuevos, ct)
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
                        log.LogError(ex, "Error al consultar clientes nuevos del mes");
                        var detail = "No se pudo obtener el reporte de clientes nuevos del mes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesNuevosMes")
            .WithSummary(
                "Solo lectura: consulta equivalente a CreditoBL.ReporteClientesNuevosMes (Cliente.FechaRegistro en rango; crédito DES). oficinaId obligatorio = vendix:oficina_id. fechaIni/fechaFin opcionales (mes calendario actual vía usp_FechaBD). usuarioId opcional. CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptCreditoObservadoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-nuevos-mes-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesNuevosMesReadService clientesNuevosMes,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    IDatabaseTimeProvider databaseTime,
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

                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    DateTime inicio;
                    DateTime fin;
                    if (fechaIni is null || fechaFin is null)
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor para el periodo por defecto.");
                        }

                        var fecha = serverTime.Value.Date;
                        inicio = fecha.AddDays(-fecha.Day + 1);
                        fin = inicio.AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        if (fechaFin.Value.Date < fechaIni.Value.Date)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Parámetros inválidos",
                                detail: "fechaFin no puede ser anterior a fechaIni.");
                        }

                        inicio = fechaIni.Value.Date;
                        fin = fechaFin.Value.Date;
                    }

                    var (oficinaResolvedNuevosCsv, usuarioResolvedNuevosCsv, accessNuevosCsvErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessNuevosCsvErr is not null)
                    {
                        return accessNuevosCsvErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesNuevosMesCsv");
                    try
                    {
                        var items = await clientesNuevosMes
                            .ListarAsync(inicio, fin, oficinaResolvedNuevosCsv, usuarioResolvedNuevosCsv, ct)
                            .ConfigureAwait(false);
                        var bytes = RptCreditoObservadoCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "clientes-nuevos-mes.csv");
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
                        log.LogError(ex, "Error al consultar clientes nuevos del mes (CSV)");
                        var detail = "No se pudo generar el CSV de clientes nuevos del mes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesNuevosMesCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-nuevos-mes en CSV UTF-8 (BOM). CreditoBL.ReporteClientesNuevosMes sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-clientes-nuevos-mes-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    int? oficinaId,
                    int? usuarioId,
                    IRptClientesNuevosMesReadService clientesNuevosMes,
                    IUsuarioAdminReadService usuarios,
                    IOficinaReadService oficinas,
                    IDatabaseTimeProvider databaseTime,
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

                    if (usuarioId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "usuarioId, si se indica, debe ser un entero >= 1.");
                    }

                    DateTime inicio;
                    DateTime fin;
                    if (fechaIni is null || fechaFin is null)
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor para el periodo por defecto.");
                        }

                        var fecha = serverTime.Value.Date;
                        inicio = fecha.AddDays(-fecha.Day + 1);
                        fin = inicio.AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        if (fechaFin.Value.Date < fechaIni.Value.Date)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Parámetros inválidos",
                                detail: "fechaFin no puede ser anterior a fechaIni.");
                        }

                        inicio = fechaIni.Value.Date;
                        fin = fechaFin.Value.Date;
                    }

                    var (oficinaResolvedNuevosCsv, usuarioResolvedNuevosCsv, accessNuevosCsvErr) =
                        await GestorInformeReportAccess.ResolveAsync(httpContext, oficinas, oficinaId, usuarioId, ct).ConfigureAwait(false);
                    if (accessNuevosCsvErr is not null)
                    {
                        return accessNuevosCsvErr;
                    }

                    var log = loggerFactory.CreateLogger("RptClientesNuevosMesPdf");
                    try
                    {
                        var items = await clientesNuevosMes
                            .ListarAsync(inicio, fin, oficinaResolvedNuevosCsv, usuarioResolvedNuevosCsv, ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptCreditoObservadoCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await GestorInformePdfContextBuilder
                            .BuildClientesNuevosMesAsync(
                                oficinaResolvedNuevosCsv,
                                usuarioResolvedNuevosCsv,
                                inicio,
                                fin,
                                usuarios,
                                oficinas,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            "Clientes nuevos del mes",
                            csvBytes,
                            context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "clientes-nuevos-mes.pdf");
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
                        log.LogError(ex, "Error al consultar clientes nuevos del mes (PDF)");
                        var detail = "No se pudo generar el PDF de clientes nuevos del mes.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptClientesNuevosMesPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-clientes-nuevos-mes en PDF tabular (QuestPDF). CreditoBL.ReporteClientesNuevosMes sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
