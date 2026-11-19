using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Credito;
using Credito.Modern.Api.Hosting;
using Credito.Modern.Api.Reportes;
using Credito.Modern.Api.Validation;
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
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.Ventas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Ventas;

/// <summary>Puntos y reportes de venta.</summary>
internal static partial class VentasEndpoints
{
    private static void MapVentasPuntosYReportes(WebApplication app)
    {
        app.MapGet(
                "/api/v1/ventas/tarjeta-puntos",
                async Task<Results<Ok<TarjetaPuntoDto>, ProblemHttpResult>> (
                    int? personaId,
                    ICanjearPuntosService canje,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("VentasTarjetaPuntos");
                    try
                    {
                        var tarjeta = await canje.ObtenerTarjetaAsync(personaId.Value, ct).ConfigureAwait(false);
                        if (tarjeta is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "El cliente no tiene tarjeta de puntos activa.");
                        }

                        return TypedResults.Ok(tarjeta);
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
                        log.LogError(ex, "Error al obtener tarjeta de puntos");
                        var detail = "No se pudo obtener la tarjeta de puntos.";
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
            .WithName("VentasTarjetaPuntos")
            .WithSummary("Paridad CanjearPuntosController.ObtenerPuntos.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TarjetaPuntoDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/articulos-canjear",
                async Task<Results<Ok<List<ArticuloCanjeListItemDto>>, ProblemHttpResult>> (
                    int? personaId,
                    ICanjearPuntosService canje,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (personaId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "personaId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("VentasArticulosCanjear");
                    try
                    {
                        var items = await canje
                            .ListarArticulosCanjeablesAsync(personaId.Value, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar artículos canjeables");
                        var detail = "No se pudo listar artículos canjeables.";
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
            .WithName("VentasArticulosCanjear")
            .WithSummary(
                "Paridad CanjearPuntosController.Listar / ArticuloBL.LstListaArticulosJGrid (PuntosCanje <= TotalPuntos, IndCanjeable).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<ArticuloCanjeListItemDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/canjear-puntos",
                async Task<Results<Ok<CanjearPuntosResponse>, ProblemHttpResult>> (
                    CanjearPuntosRequest body,
                    ICanjearPuntosService canje,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var canjeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            IdRules.RequirePositive(body.PersonaId, "personaId"),
                            StringRules.RequireText(body.NumeroSerie, "numeroSerie", StringRules.MaxSerie)));
                    if (canjeError is not null)
                        return canjeError;

                    var log = loggerFactory.CreateLogger("VentasCanjearPuntos");
                    try
                    {
                        var mensaje = await canje
                            .CanjearAsync(body.PersonaId, body.NumeroSerie, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new CanjearPuntosResponse(mensaje));
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Solicitud inválida al canjear puntos");
                        return ProblemResults.FromArgument(ex);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al canjear puntos");
                        var detail = "No se pudo completar el canje.";
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
            .WithName("VentasCanjearPuntos")
            .WithSummary("Paridad CanjearPuntosController.CanjearArticulo (usp_CanjearPuntos). Mensaje vacío = éxito.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CanjearPuntosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/rpt-rentabilidad-venta-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    bool? indContado,
                    bool? indCredito,
                    int? oficinaId,
                    IRptRentabilidadVentaReadService rentabilidad,
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

                    var indC = indContado ?? false;
                    var indCr = indCredito ?? false;

                    var log = loggerFactory.CreateLogger("RptRentabilidadVentaCsv");
                    try
                    {
                        var items = await rentabilidad.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                indC,
                                indCr,
                                oficinaId.Value,
                                ct)
                            .ConfigureAwait(false);
                        var bytes = RptRentabilidadVentaCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "rentabilidad-venta.csv");
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_RptRentabilidadVenta (CSV)");
                        var detail = "No se pudo generar el CSV de rentabilidad de ventas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRptRentabilidadVentaCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-rentabilidad-venta en CSV UTF-8 (BOM). VENTAS.usp_RptRentabilidadVenta sin motor RDLC. CreditoUser.")
            .WithTags("ventas", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/rpt-rentabilidad-venta-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    bool? indContado,
                    bool? indCredito,
                    int? oficinaId,
                    IRptRentabilidadVentaReadService rentabilidad,
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

                    var indC = indContado ?? false;
                    var indCr = indCredito ?? false;

                    var log = loggerFactory.CreateLogger("RptRentabilidadVentaPdf");
                    try
                    {
                        var items = await rentabilidad.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                indC,
                                indCr,
                                oficinaId.Value,
                                ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptRentabilidadVentaCsvFormatter.ToUtf8BomCsv(items);
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(
                                httpContext,
                                oficinaId.Value,
                                fechaIni: fechaIni.Value,
                                fechaFin: fechaFin.Value,
                                titulo: "RENTABILIDAD DE VENTAS",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            "Rentabilidad venta",
                            csvBytes,
                            context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "rentabilidad-venta.pdf");
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_RptRentabilidadVenta (PDF)");
                        var detail = "No se pudo generar el PDF de rentabilidad de ventas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRptRentabilidadVentaPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-rentabilidad-venta en PDF tabular (QuestPDF). VENTAS.usp_RptRentabilidadVenta sin motor RDLC. CreditoUser.")
            .WithTags("ventas", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/rpt-lista-precio",
                async Task<Results<Ok<List<RptListaPrecioGeneralRowDto>>, ProblemHttpResult>> (
                    int? marcaId,
                    bool? indDescuento,
                    bool? indPuntos,
                    IRptListaPrecioGeneralReadService listaPrecioGeneral,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (marcaId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "marcaId, si se indica, debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptListaPrecioGeneral");
                    try
                    {
                        var items = await listaPrecioGeneral
                            .ListarAsync(marcaId, indDescuento == true, indPuntos == true, ct)
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
                        log.LogWarning(ex, "marcaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al consultar lista de precios (informe)");
                        var detail = "No se pudo obtener el informe de lista de precios.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRptListaPrecio")
            .WithSummary(
                "Solo lectura: consulta equivalente a ReporteBL.ListarReporteListaPrecio (ListaPrecio activa; filtros marcaId, indDescuento, indPuntos). CreditoUser. Distinto de GET /api/v1/lista-precios (catálogo).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptListaPrecioGeneralRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/rpt-lista-precio-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int? marcaId,
                    bool? indDescuento,
                    bool? indPuntos,
                    IRptListaPrecioGeneralReadService listaPrecioGeneral,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (marcaId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "marcaId, si se indica, debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptListaPrecioCsv");
                    try
                    {
                        var items = await listaPrecioGeneral
                            .ListarAsync(marcaId, indDescuento == true, indPuntos == true, ct)
                            .ConfigureAwait(false);
                        var bytes = RptListaPrecioCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "lista-precio.csv");
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
                        log.LogWarning(ex, "marcaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al consultar lista de precios (CSV)");
                        var detail = "No se pudo generar el CSV de lista de precios.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRptListaPrecioCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-lista-precio en CSV UTF-8 (BOM). Paridad datos ReporteListaPrecio / rptListaPrecio.rdlc sin motor RDLC. CreditoUser.")
            .WithTags("ventas", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/rpt-lista-precio-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? marcaId,
                    bool? indDescuento,
                    bool? indPuntos,
                    IRptListaPrecioGeneralReadService listaPrecioGeneral,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (marcaId is < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "marcaId, si se indica, debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("RptListaPrecioPdf");
                    try
                    {
                        var items = await listaPrecioGeneral
                            .ListarAsync(marcaId, indDescuento == true, indPuntos == true, ct)
                            .ConfigureAwait(false);
                        var csvBytes = RptListaPrecioCsvFormatter.ToUtf8BomCsv(items);
                        int? oficinaId = MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oid)
                            ? oid
                            : null;
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(
                                httpContext,
                                oficinaId,
                                referencia: marcaId is > 0 ? $"Marca #{marcaId}" : null,
                                titulo: "LISTA DE PRECIOS",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv(
                            "Lista de precios",
                            csvBytes,
                            context: pdfContext);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "lista-precio.pdf");
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
                        log.LogWarning(ex, "marcaId inválido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al consultar lista de precios (PDF)");
                        var detail = "No se pudo generar el PDF de lista de precios.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRptListaPrecioPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-lista-precio en PDF tabular (QuestPDF). Paridad datos ReporteListaPrecio / rptListaPrecio.rdlc sin motor RDLC. CreditoUser.")
            .WithTags("ventas", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet("/api/v1/ventas/codigo-barras-lst", async Task<Results<Ok<List<CodigoBarrasLstRowDto>>, ProblemHttpResult>> (
            int? movimientoId,
            ICodigoBarrasLstReadService codigoBarras,
            ILoggerFactory loggerFactory,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (movimientoId is null or < 1)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                    detail: "movimientoId es obligatorio y debe ser un entero >= 1.");
            }

            var log = loggerFactory.CreateLogger("CodigoBarrasLst");
            try
            {
                var items = await codigoBarras.ListarPorMovimientoAsync(movimientoId.Value, ct).ConfigureAwait(false);
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
                log.LogWarning(ex, "movimientoId inválido");
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
            }
            catch (DbException ex)
            {
                log.LogError(ex, "Error al ejecutar VENTAS.usp_CodigoBarras_Lst");
                var detail = "No se pudo ejecutar la lista de códigos de barras.";
                if (env.IsDevelopment())
                    detail += $" Detalle: {ex.Message}";
                return TypedResults.Problem(
                    detail: detail,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Error de base de datos");
            }
        })
        .WithName("VentasCodigoBarrasLst")
        .WithTags("read-only")
        .WithSummary("Solo lectura: VENTAS.usp_CodigoBarras_Lst(pMovimientoId). movimientoId obligatorio (>=1).")
        .Produces<List<CodigoBarrasLstRowDto>>(StatusCodes.Status200OK, "application/json")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/codigo-barras-lst-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    int? movimientoId,
                    ICodigoBarrasLstReadService codigoBarras,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "movimientoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CodigoBarrasLstCsv");
                    try
                    {
                        var items = await codigoBarras.ListarPorMovimientoAsync(movimientoId.Value, ct).ConfigureAwait(false);
                        var bytes = CodigoBarrasLstCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", $"codigo-barras-{movimientoId}.csv");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        if (ex is InvalidOperationException ioe)
                        {
                            log.LogWarning(ioe, "Configuración");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta");
                        }

                        log.LogError(ex, "Error CSV codigo barras");
                        var detail = "No se pudo exportar códigos de barras.";
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
            .WithName("VentasCodigoBarrasLstCsv")
            .WithTags("read-only", "ventas-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .WithSummary("Export: VENTAS.usp_CodigoBarras_Lst en CSV. Paridad SerieArticuloBL.ListarArticuloCodigoBarras.")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");



        app.MapGet(
                "/api/v1/ventas/codigo-barras-lst-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? movimientoId,
                    ICodigoBarrasLstReadService codigoBarras,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (movimientoId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "movimientoId es obligatorio y debe ser un entero >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("CodigoBarrasLstPdf");
                    try
                    {
                        var items = await codigoBarras.ListarPorMovimientoAsync(movimientoId.Value, ct).ConfigureAwait(false);
                        var csvBytes = CodigoBarrasLstCsvFormatter.ToUtf8BomCsv(items);
                        int? oficinaId = MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var oid)
                            ? oid
                            : null;
                        var pdfContext = await LegacyReportPdf
                            .ResolveAsync(
                                httpContext,
                                oficinaId,
                                referencia: $"Movimiento #{movimientoId.Value}",
                                titulo: "CÓDIGOS DE BARRAS",
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                        var pdfBytes = TabularPdfDocument.FromUtf8BomCsv(
                            $"Códigos de barras mov. {movimientoId}",
                            csvBytes,
                            context: pdfContext);
                        return TypedResults.File(pdfBytes, "application/pdf", $"codigo-barras-{movimientoId}.pdf");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        if (ex is InvalidOperationException ioe)
                        {
                            log.LogWarning(ioe, "Configuración");
                            return TypedResults.Problem(
                                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta");
                        }

                        log.LogError(ex, "Error PDF codigo barras");
                        var detail = "No se pudo exportar códigos de barras.";
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
            .WithName("VentasCodigoBarrasLstPdf")
            .WithTags("read-only", "ventas-informes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .WithSummary("Export PDF tabular: mismos datos que codigo-barras-lst.")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");



        app.MapGet(
                "/api/v1/ventas/rpt-rentabilidad-venta",
                async Task<Results<Ok<List<RptRentabilidadVentaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    DateTime? fechaIni,
                    DateTime? fechaFin,
                    bool? indContado,
                    bool? indCredito,
                    int? oficinaId,
                    IRptRentabilidadVentaReadService rentabilidad,
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

                    var indC = indContado ?? false;
                    var indCr = indCredito ?? false;

                    var log = loggerFactory.CreateLogger("RptRentabilidadVenta");
                    try
                    {
                        var items = await rentabilidad.ListarAsync(
                                fechaIni.Value,
                                fechaFin.Value,
                                indC,
                                indCr,
                                oficinaId.Value,
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_RptRentabilidadVenta");
                        var detail = "No se pudo obtener el reporte de rentabilidad de ventas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRptRentabilidadVenta")
            .WithSummary(
                "Solo lectura: VENTAS.usp_RptRentabilidadVenta(FechaIni, FechaFin, IndContado, IndCredito, OficinaId). fechaIni/fechaFin obligatorias; oficinaId obligatorio y debe coincidir con vendix:oficina_id. indContado/indCredito opcionales (default false). CreditoUser. El MVC permitía oficina null (todas); aquí solo la oficina del token (ReporteBL.ListarReporteRentabilidadVenta).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptRentabilidadVentaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



    }
}