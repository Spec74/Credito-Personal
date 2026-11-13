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

/// <summary>Aval, saldos caja, movimiento bóveda, resumen cuenta y métricas vencimiento.</summary>
internal static class CreditoReporteCajaBovedaEndpoints
{
    public static void MapCreditoReporteCajaBovedaEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/rpt-aval",
                async Task<Results<Ok<List<RptAvalRowDto>>, ProblemHttpResult>> (
                    int? personaId,
                    IRptAvalReadService rptAval,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptAval");
                    try
                    {
                        var items = await rptAval.ListarPorPersonaAsync(personaId.Value, ct).ConfigureAwait(false);
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
                        log.LogWarning(ex, "personaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptAval");
                        var detail = "No se pudo obtener el reporte de avales por persona.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptAval")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptAval(PersonaId). personaId obligatorio (>=1). CreditoUser. Equivale a CreditoBL.ReporteAval.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptAvalRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-aval-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int? personaId,
                    IRptAvalReadService rptAval,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptAvalCsv");
                    try
                    {
                        var items = await rptAval.ListarPorPersonaAsync(personaId.Value, ct).ConfigureAwait(false);
                        var bytes = RptAvalCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "aval-persona.csv");
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
                        log.LogWarning(ex, "personaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptAval (CSV)");
                        var detail = "No se pudo generar el CSV de avales por persona.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptAvalCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-aval en CSV UTF-8 (BOM). CREDITO.usp_RptAval sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-aval-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? personaId,
                    IRptAvalReadService rptAval,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptAvalPdf");
                    try
                    {
                        var items = await rptAval.ListarPorPersonaAsync(personaId.Value, ct).ConfigureAwait(false);
                        var csvBytes = RptAvalCsvFormatter.ToUtf8BomCsv(items);
                        int? oficinaId = MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oid)
                            ? oid
                            : null;
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(
                                httpContext,
                                oficinaId,
                                referencia: $"Persona N° {personaId.Value}",
                                titulo: "AVALES POR PERSONA",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Avales", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "aval-persona.pdf");
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
                        log.LogWarning(ex, "personaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptAval (PDF)");
                        var detail = "No se pudo generar el PDF de avales por persona.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptAvalPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-aval en PDF tabular (QuestPDF). CREDITO.usp_RptAval sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldos-caja",
                async Task<Results<Ok<List<RptSaldosCajaRowDto>>, ProblemHttpResult>> (
                    int? cajaDiarioId,
                    bool? indCajaChica,
                    bool? incluirAnulados,
                    IRptSaldosCajaReadService rptSaldos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "cajaDiarioId es obligatorio y debe ser un entero >= 1.");
                    }

                    var indChica = indCajaChica == true;
                    var log = loggerFactory.CreateLogger("RptSaldosCaja");
                    try
                    {
                        List<RptSaldosCajaRowDto> items;
                        if (!indChica && incluirAnulados == true)
                        {
                            items = await rptSaldos
                                .ListarArqueoAsync(cajaDiarioId.Value, incluirAnulados: true, ct)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            items = await rptSaldos.ListarAsync(cajaDiarioId.Value, indChica, ct).ConfigureAwait(false);
                        }

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
                        log.LogWarning(ex, "cajaDiarioId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldosCaja");
                        var detail = "No se pudo obtener el reporte de saldos de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptSaldosCaja")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptSaldosCaja o arqueo con anulados (incluirAnulados). PDF/CSV no usan anulados.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptSaldosCajaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldos-caja-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int? cajaDiarioId,
                    bool? indCajaChica,
                    IRptSaldosCajaReadService rptSaldos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "cajaDiarioId es obligatorio y debe ser un entero >= 1.");
                    }

                    var indChica = indCajaChica == true;
                    var log = loggerFactory.CreateLogger("RptSaldosCajaCsv");
                    try
                    {
                        var items = await rptSaldos.ListarAsync(cajaDiarioId.Value, indChica, ct).ConfigureAwait(false);
                        var bytes = RptSaldosCajaCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "saldos-caja.csv");
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
                        log.LogWarning(ex, "cajaDiarioId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldosCaja (CSV)");
                        var detail = "No se pudo generar el CSV de saldos de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptSaldosCajaCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-saldos-caja en CSV UTF-8 (BOM). usp_RptSaldosCaja sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldos-caja-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? cajaDiarioId,
                    bool? indCajaChica,
                    IRptSaldosCajaReadService rptSaldos,
                    IRptSaldoCajaCabReadService cabSaldos,
                    IRptSaldosCajaResumenIngresoReadService resumenIngreso,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "cajaDiarioId es obligatorio y debe ser un entero >= 1.");
                    }

                    var indChica = indCajaChica == true;
                    var log = loggerFactory.CreateLogger("RptSaldosCajaPdf");
                    try
                    {
                        var items = await rptSaldos.ListarAsync(cajaDiarioId.Value, indChica, ct).ConfigureAwait(false);
                        var cab = await cabSaldos
                            .ObtenerAsync(cajaDiarioId.Value, indChica, ct)
                            .ConfigureAwait(false);
                        string? resumen = null;
                        if (!indChica)
                        {
                            // usp_RptSaldosCajaResumenIngreso usa ufnResumenCuentaCajaDiario(caja);
                            // oficinaId es obligatorio en el contrato API pero no altera el texto.
                            var oficinaResumen = MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOid)
                                && jwtOid > 0
                                    ? jwtOid
                                    : 1;
                            resumen = await resumenIngreso
                                .ObtenerPrimerTextoAsync(cajaDiarioId.Value, oficinaResumen, ct)
                                .ConfigureAwait(false);
                        }

                        var bytes = RptSaldosCajaPdfDocument.Build(items, cab, resumen, indChica);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "saldos-caja.pdf");
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
                        log.LogWarning(ex, "cajaDiarioId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldosCaja (PDF)");
                        var detail = "No se pudo generar el PDF de saldos de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptSaldosCajaPdf")
            .WithSummary(
                "Export lectura: paridad rptSaldoCaja.rdlc (QuestPDF). usp_RptSaldosCaja + cabecera + resumen efectivo/digitales (ufnResumenCuentaCajaDiario). CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldos-caja-resumen-ingreso",
                async Task<Results<Ok<RptSaldosCajaResumenIngresoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? cajaDiarioId,
                    int? oficinaId,
                    IRptSaldosCajaResumenIngresoReadService resumenIngreso,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaDiarioId is null or < 1 || oficinaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "cajaDiarioId y oficinaId son obligatorios y deben ser enteros >= 1.");
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

                    var log = loggerFactory.CreateLogger("RptSaldosCajaResumenIngreso");
                    try
                    {
                        var texto = await resumenIngreso
                            .ObtenerPrimerTextoAsync(cajaDiarioId.Value, oficinaId.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new RptSaldosCajaResumenIngresoResponse(texto));
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldosCajaResumenIngreso");
                        var detail = "No se pudo obtener el resumen de ingresos de caja.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptSaldosCajaResumenIngreso")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptSaldosCajaResumenIngreso(CajaDiarioId, OficinaId). cajaDiarioId y oficinaId obligatorios; oficinaId debe coincidir con vendix:oficina_id del JWT. CreditoUser. texto = primera fila (como CajaDiarioBL.ObtenerResumenIngresoCajaDiario / ReporteController).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RptSaldosCajaResumenIngresoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-saldos-caja-resumen-tipo-cuenta",
                async Task<Results<Ok<RptSaldosCajaResumenTipoCuentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    IRptSaldosCajaResumenTipoCuentaReadService resumenTipoCuenta,
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

                    var log = loggerFactory.CreateLogger("RptSaldosCajaResumenTipoCuenta");
                    try
                    {
                        var texto = await resumenTipoCuenta.ObtenerPrimerTextoAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new RptSaldosCajaResumenTipoCuentaResponse(texto));
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptSaldosCajaResumenTipoCuenta");
                        var detail = "No se pudo obtener el resumen de saldos por tipo de cuenta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptSaldosCajaResumenTipoCuenta")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptSaldosCajaResumenTipoCuenta(OficinaId). oficinaId obligatorio y debe coincidir con vendix:oficina_id del JWT. CreditoUser. texto = primera fila (como CajaDiarioBL.ObtenerResumenCuentaCajaDiarios / ReporteController).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RptSaldosCajaResumenTipoCuentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-boveda",
                async Task<Results<Ok<List<RptMovimientoBovedaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? bovedaId,
                    IBovedaOficinaReadService bovedaOficina,
                    IRptMovimientoBovedaReadService rptMovimientoBoveda,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "bovedaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoBoveda");
                    int? oficinaBoveda;
                    try
                    {
                        oficinaBoveda = await bovedaOficina.GetOficinaIdByBovedaIdAsync(bovedaId.Value, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error SQL al resolver CREDITO.Boveda por BovedaId");
                        var detail = "No se pudo validar la bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }

                    if (oficinaBoveda is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una bóveda con el bovedaId indicado.");
                    }

                    if (oficinaBoveda.Value != jwtOficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La bóveda no pertenece a la oficina del token JWT.");
                    }

                    try
                    {
                        var items = await rptMovimientoBoveda.ListarAsync(bovedaId.Value, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoBoveda");
                        var detail = "No se pudo obtener el movimiento de bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptMovimientoBoveda")
            .WithSummary(
                "Solo lectura: CREDITO.usp_RptMovimientoBoveda(BovedaId). bovedaId obligatorio; la bóveda debe existir en CREDITO.Boveda y su OficinaId debe coincidir con vendix:oficina_id del JWT. CreditoUser. Equivale a BovedaBL.ReporteMovimientoBoveda.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptMovimientoBovedaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-boveda-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? bovedaId,
                    IBovedaOficinaReadService bovedaOficina,
                    IRptMovimientoBovedaReadService rptMovimientoBoveda,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "bovedaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoBovedaCsv");
                    int? oficinaBoveda;
                    try
                    {
                        oficinaBoveda = await bovedaOficina.GetOficinaIdByBovedaIdAsync(bovedaId.Value, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error SQL al resolver CREDITO.Boveda por BovedaId (CSV)");
                        var detail = "No se pudo validar la bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }

                    if (oficinaBoveda is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una bóveda con el bovedaId indicado.");
                    }

                    if (oficinaBoveda.Value != jwtOficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La bóveda no pertenece a la oficina del token JWT.");
                    }

                    try
                    {
                        var items = await rptMovimientoBoveda.ListarAsync(bovedaId.Value, ct).ConfigureAwait(false);
                        var bytes = RptMovimientoBovedaCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "movimiento-boveda.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoBoveda (CSV)");
                        var detail = "No se pudo generar el CSV de movimiento de bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptMovimientoBovedaCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-movimiento-boveda en CSV UTF-8 (BOM). usp_RptMovimientoBoveda sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/rpt-movimiento-boveda-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? bovedaId,
                    IBovedaOficinaReadService bovedaOficina,
                    IRptMovimientoBovedaReadService rptMovimientoBoveda,
                    IResumenCuentaBovedaReadService resumenCuentaBoveda,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "bovedaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var log = loggerFactory.CreateLogger("RptMovimientoBovedaPdf");
                    int? oficinaBoveda;
                    try
                    {
                        oficinaBoveda = await bovedaOficina.GetOficinaIdByBovedaIdAsync(bovedaId.Value, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error SQL al resolver CREDITO.Boveda por BovedaId (PDF)");
                        var detail = "No se pudo validar la bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }

                    if (oficinaBoveda is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una bóveda con el bovedaId indicado.");
                    }

                    if (oficinaBoveda.Value != jwtOficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La bóveda no pertenece a la oficina del token JWT.");
                    }

                    try
                    {
                        var items = await rptMovimientoBoveda.ListarAsync(bovedaId.Value, ct).ConfigureAwait(false);
                        var cab = await bovedaOficina
                            .GetCabeceraReporteAsync(bovedaId.Value, ct)
                            .ConfigureAwait(false);
                        var resumenCuenta = await resumenCuentaBoveda
                            .ObtenerPrimerTextoAsync(bovedaId.Value, ct)
                            .ConfigureAwait(false);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                jwtOficinaId,
                                referencia: $"Bóveda N° {bovedaId.Value}",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = RptMovimientoBovedaPdfDocument.Build(items, pdfContext, cab, resumenCuenta);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "movimiento-boveda.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_RptMovimientoBoveda (PDF)");
                        var detail = "No se pudo generar el PDF de movimiento de bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoRptMovimientoBovedaPdf")
            .WithSummary(
                "Export lectura: paridad rptMovimientoBoveda.rdlc (QuestPDF). usp_RptMovimientoBoveda + cabecera de saldos + usp_ResumenCuentaBoveda (efectivo / medios digitales). CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/resumen-cuenta-boveda",
                async Task<Results<Ok<ResumenCuentaBovedaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? bovedaId,
                    IBovedaOficinaReadService bovedaOficina,
                    IResumenCuentaBovedaReadService resumenCuentaBoveda,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "bovedaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var log = loggerFactory.CreateLogger("ResumenCuentaBoveda");
                    int? oficinaBoveda;
                    try
                    {
                        oficinaBoveda = await bovedaOficina.GetOficinaIdByBovedaIdAsync(bovedaId.Value, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error SQL al resolver CREDITO.Boveda por BovedaId");
                        var detail = "No se pudo validar la bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }

                    if (oficinaBoveda is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una bóveda con el bovedaId indicado.");
                    }

                    if (oficinaBoveda.Value != jwtOficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La bóveda no pertenece a la oficina del token JWT.");
                    }

                    try
                    {
                        var texto = await resumenCuentaBoveda.ObtenerPrimerTextoAsync(bovedaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new ResumenCuentaBovedaResponse(texto));
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ResumenCuentaBoveda");
                        var detail = "No se pudo obtener el resumen de cuenta de bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoResumenCuentaBoveda")
            .WithSummary(
                "Solo lectura: CREDITO.usp_ResumenCuentaBoveda(BovedaId). bovedaId obligatorio; misma validación de oficina que rpt-movimiento-boveda. CreditoUser. texto = primera fila (como BovedaBL.ResumenCuentaBoveda).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ResumenCuentaBovedaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/metricas-vencimiento-credito",
                async Task<Results<Ok<CreditoVencidoMetricasDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? creditoId,
                    ICreditoOficinaReadService creditoOficina,
                    ICreditoVencidoMetricasReadService creditoVencidoMetricas,
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

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida (vendix:oficina_id).");
                    }

                    var log = loggerFactory.CreateLogger("MetricasVencimientoCredito");
                    int? oficinaCredito;
                    try
                    {
                        oficinaCredito = await creditoOficina.GetOficinaIdByCreditoIdAsync(creditoId.Value, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error SQL al resolver CREDITO.Credito por CreditoId");
                        var detail = "No se pudo validar el crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }

                    if (oficinaCredito is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe un crédito con el creditoId indicado.");
                    }

                    if (oficinaCredito.Value != jwtOficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El crédito no pertenece a la oficina del token JWT.");
                    }

                    try
                    {
                        var metricas = await creditoVencidoMetricas.ObtenerAsync(creditoId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(metricas ?? new CreditoVencidoMetricasDto());
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
                        log.LogError(ex, "Error al ejecutar CREDITO.uspCreditoVencido");
                        var detail = "No se pudieron obtener las métricas de vencimiento del crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoMetricasVencimientoCredito")
            .WithSummary(
                "Solo lectura: CREDITO.uspCreditoVencido(CreditoId). creditoId obligatorio; CREDITO.Credito.OficinaId debe coincidir con vendix:oficina_id del JWT (404/403). CreditoUser. Equivale a BovedaBL.CreditoVencido (montos vencido / franjas).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CreditoVencidoMetricasDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
