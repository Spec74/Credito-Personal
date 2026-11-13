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

/// <summary>Central de riesgo + listar saldo cartera (JSON/CSV/PDF/TXT).</summary>
internal static class CreditoReporteCentralRiesgoSaldoCarteraEndpoints
{
    public static void MapCreditoReporteCentralRiesgoSaldoCarteraEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/central-riesgo-generar-txt",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? anio,
                    int? mes,
                    bool? regenerar,
                    ICentralRiesgoGenerarReadService centralRiesgo,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || anio is null || mes is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, anio y mes son obligatorios (anio 1900â€“2100, mes 1â€“12).");
                    }

                    if (anio.Value < 1900 || anio.Value > 2100 || mes.Value < 1 || mes.Value > 12)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anio debe estar entre 1900 y 2100 y mes entre 1 y 12.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("CentralRiesgoGenerarTxt");
                    try
                    {
                        var items = await centralRiesgo.ListarAsync(oficinaId.Value, anio.Value, mes.Value, regenerar == true, ct).ConfigureAwait(false);
                        var bytes = CentralRiesgoGenerarTxtFormatter.ToUtf8Bytes(items);
                        return TypedResults.File(bytes, "text/plain; charset=utf-8", fileDownloadName: $"DM007898_O{oficinaId.Value}_{anio.Value}{mes.Value:D2}.txt");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CentralRiesgoGenerar (TXT)");
                        var detail = "No se pudo generar el archivo TXT de central de riesgos.";
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
            .WithName("CreditoCentralRiesgoGenerarTxt")
            .WithSummary(
                "Export TXT DM007898 (paridad ReporteController.ReporteCentrarRiegoTXT / padding legacy).")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/plain")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/listar-saldo-cartera",
                async Task<Results<Ok<List<ListarSaldoCarteraRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? anio,
                    int? mes,
                    int? oficinaId,
                    int? usuarioId,
                    IListarSaldoCarteraReadService saldoCartera,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (anio is null or < 1900 or > 2100
                        || mes is null or < 1 or > 12
                        || oficinaId is null or < 1
                        || usuarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail:
                                "anio (1900â€“2100), mes (1â€“12), oficinaId y usuarioId (>= 1) son obligatorios.");
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

                    var log = loggerFactory.CreateLogger("ListarSaldoCartera");
                    try
                    {
                        var items = await saldoCartera
                            .ListarAsync(anio.Value, mes.Value, oficinaId.Value, usuarioId.Value, ct)
                            .ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ListarSaldoCartera");
                        var detail = "No se pudo obtener el saldo de cartera.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoListarSaldoCartera")
            .WithSummary(
                "Solo lectura: CREDITO.usp_ListarSaldoCartera(Anio, Mes, OficinaId, UsuarioId). Parámetros obligatorios; oficinaId y usuarioId deben coincidir con el JWT. CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ListarSaldoCarteraRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/listar-saldo-cartera-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? anio,
                    int? mes,
                    int? oficinaId,
                    int? usuarioId,
                    IListarSaldoCarteraReadService saldoCartera,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (anio is null or < 1900 or > 2100
                        || mes is null or < 1 or > 12
                        || oficinaId is null or < 1
                        || usuarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail:
                                "anio (1900â€“2100), mes (1â€“12), oficinaId y usuarioId (>= 1) son obligatorios.");
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

                    var log = loggerFactory.CreateLogger("ListarSaldoCarteraCsv");
                    try
                    {
                        var items = await saldoCartera
                            .ListarAsync(anio.Value, mes.Value, oficinaId.Value, usuarioId.Value, ct)
                            .ConfigureAwait(false);
                        var bytes = ListarSaldoCarteraCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "listar-saldo-cartera.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ListarSaldoCartera (CSV)");
                        var detail = "No se pudo generar el CSV de saldo de cartera.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoListarSaldoCarteraCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET listar-saldo-cartera en CSV UTF-8 (BOM). usp_ListarSaldoCartera sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/listar-saldo-cartera-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? anio,
                    int? mes,
                    int? oficinaId,
                    int? usuarioId,
                    IListarSaldoCarteraReadService saldoCartera,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (anio is null or < 1900 or > 2100
                        || mes is null or < 1 or > 12
                        || oficinaId is null or < 1
                        || usuarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail:
                                "anio (1900â€“2100), mes (1â€“12), oficinaId y usuarioId (>= 1) son obligatorios.");
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

                    var log = loggerFactory.CreateLogger("ListarSaldoCarteraPdf");
                    try
                    {
                        var items = await saldoCartera
                            .ListarAsync(anio.Value, mes.Value, oficinaId.Value, usuarioId.Value, ct)
                            .ConfigureAwait(false);
                        var csvBytes = ListarSaldoCarteraCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaId,
                                usuarioId,
                                anio: anio,
                                mes: mes,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Saldo cartera", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "listar-saldo-cartera.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_ListarSaldoCartera (PDF)");
                        var detail = "No se pudo generar el PDF de saldo de cartera.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoListarSaldoCarteraPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET listar-saldo-cartera en PDF tabular (QuestPDF). usp_ListarSaldoCartera sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/central-riesgo-generar",
                async Task<Results<Ok<List<CentralRiesgoGenerarRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? anio,
                    int? mes,
                    bool? regenerar,
                    ICentralRiesgoGenerarReadService centralRiesgo,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || anio is null || mes is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, anio y mes son obligatorios (anio 1900â€“2100, mes 1â€“12).");
                    }

                    if (anio.Value < 1900 || anio.Value > 2100 || mes.Value < 1 || mes.Value > 12)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anio debe estar entre 1900 y 2100 y mes entre 1 y 12.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("CentralRiesgoGenerar");
                    try
                    {
                        var items = await centralRiesgo.ListarAsync(oficinaId.Value, anio.Value, mes.Value, regenerar == true, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CentralRiesgoGenerar");
                        var detail = "No se pudo generar la información de central de riesgos.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCentralRiesgoGenerar")
            .WithSummary(
                "Central de riesgos por sede: CREDITO.usp_CentralRiesgoGenerar(OficinaId, Anio, Mes, Regenerar). Cache y lectura filtrados por oficina. regenerar=true recalcula el snapshot de la sede/periodo. CreditoUser.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CentralRiesgoGenerarRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/central-riesgo-generar-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? anio,
                    int? mes,
                    bool? regenerar,
                    ICentralRiesgoGenerarReadService centralRiesgo,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || anio is null || mes is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, anio y mes son obligatorios (anio 1900â€“2100, mes 1â€“12).");
                    }

                    if (anio.Value < 1900 || anio.Value > 2100 || mes.Value < 1 || mes.Value > 12)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anio debe estar entre 1900 y 2100 y mes entre 1 y 12.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("CentralRiesgoGenerarCsv");
                    try
                    {
                        var items = await centralRiesgo.ListarAsync(oficinaId.Value, anio.Value, mes.Value, regenerar == true, ct).ConfigureAwait(false);
                        var bytes = CentralRiesgoGenerarCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: $"DM007898_O{oficinaId.Value}_{anio.Value}{mes.Value:D2}.csv");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CentralRiesgoGenerar (CSV)");
                        var detail = "No se pudo generar el CSV de central de riesgos.";
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
            .WithName("CreditoCentralRiesgoGenerarCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET central-riesgo-generar en CSV UTF-8 (BOM). usp_CentralRiesgoGenerar sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/central-riesgo-generar-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? anio,
                    int? mes,
                    bool? regenerar,
                    ICentralRiesgoGenerarReadService centralRiesgo,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    IOficinaReadService oficinas,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || anio is null || mes is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId, anio y mes son obligatorios (anio 1900â€“2100, mes 1â€“12).");
                    }

                    if (anio.Value < 1900 || anio.Value > 2100 || mes.Value < 1 || mes.Value > 12)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "anio debe estar entre 1900 y 2100 y mes entre 1 y 12.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("CentralRiesgoGenerarPdf");
                    try
                    {
                        var items = await centralRiesgo.ListarAsync(oficinaId.Value, anio.Value, mes.Value, regenerar == true, ct).ConfigureAwait(false);
                        var csvBytes = CentralRiesgoGenerarCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf.ResolveAsync(
                                httpContext,
                                oficinaId,
                                anio: anio,
                                mes: mes,
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Central de riesgo", csvBytes, context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: $"DM007898_O{oficinaId.Value}_{anio.Value}{mes.Value:D2}.pdf");
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
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CentralRiesgoGenerar (PDF)");
                        var detail = "No se pudo generar el PDF de central de riesgos.";
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
            .WithName("CreditoCentralRiesgoGenerarPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET central-riesgo-generar en PDF tabular (QuestPDF). usp_CentralRiesgoGenerar sin motor RDLC. CreditoUser.")
            .WithTags("credito", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
