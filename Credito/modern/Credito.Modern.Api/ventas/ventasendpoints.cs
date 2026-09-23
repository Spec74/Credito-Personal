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
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Ventas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Ventas;

internal static class VentasEndpoints
{
    public static void MapVentasEndpoints(this WebApplication app)
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
                    if (body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "personaId debe ser >= 1.");
                    }

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
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
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
                            detail: "Los parámetros enviados no son válidos.");
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
                            detail: "Los parámetros enviados no son válidos.");
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
                            detail: "Los parámetros enviados no son válidos.");
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
                            detail: "Los parámetros enviados no son válidos.");
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
                            detail: "Los parámetros enviados no son válidos.");
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
                            detail: "Los parámetros enviados no son válidos.");
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
                            detail: "Los parámetros enviados no son válidos.");
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



        app.MapGet(
                "/api/v1/ventas/ordenes-venta",
                async Task<Results<Ok<OrdenVentaListPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    bool entregado,
                    string? buscar,
                    int page,
                    int pageSize,
                    IOrdenVentaReadService ordenVentaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ListarOrdenesVenta");
                    try
                    {
                        var response = await ordenVentaRead
                            .ListarAsync(oficinaId, entregado, buscar, page < 1 ? 1 : page, pageSize < 1 ? 25 : pageSize, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar órdenes de venta");
                        var detail = "No se pudo listar órdenes de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasListarOrdenesVenta")
            .WithSummary("Paridad OrdenVentaController.ListarOrdenesVentaJgrid.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaListPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/orden-venta/{ordenVentaId:int}",
                async Task<Results<Ok<OrdenVentaDetalleResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int ordenVentaId,
                    int oficinaId,
                    IOrdenVentaReadService ordenVentaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || ordenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ObtenerOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaRead.ObtenerDetalleAsync(ordenVentaId, ct).ConfigureAwait(false);
                        if (response is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe la orden de venta indicada.");
                        }

                        if (response.Cabecera.OficinaId != oficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "La orden de venta no pertenece a la oficina del token JWT.");
                        }

                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al obtener orden de venta");
                        var detail = "No se pudo obtener la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasObtenerOrdenVentaDetalle")
            .WithSummary("Paridad OrdenVentaController.BuscarOrdenVentaDet.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaDetalleResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/crear-orden-venta",
                async Task<Results<Ok<CrearOrdenVentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearOrdenVentaRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y personaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("CrearOrdenVenta");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await ordenVentaWrite
                            .CrearAsync(
                                body.OficinaId,
                                body.PersonaId,
                                usuarioId,
                                serverTime.Value,
                                body.TipoVenta ?? "CON",
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear orden de venta");
                        var detail = "No se pudo crear la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasCrearOrdenVenta")
            .WithSummary("Paridad OrdenVentaController.Index (id=0, pPersonaId>0).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CrearOrdenVentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/agregar-orden-venta-detalle",
                async Task<Results<Ok<OrdenVentaOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AgregarOrdenVentaDetalleRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaOficinaReadService ordenVentaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1 || string.IsNullOrWhiteSpace(body.NumeroSerie))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, ordenVentaId y numeroSerie son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var ordenOid = await ordenVentaOficina
                        .GetOficinaIdByOrdenVentaIdAsync(body.OrdenVentaId, ct)
                        .ConfigureAwait(false);
                    if (ordenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (ordenOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("AgregarOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaWrite
                            .AgregarDetalleAsync(body.OrdenVentaId, body.NumeroSerie, usuarioId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVentaDet_Ins");
                        var detail = "No se pudo agregar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasAgregarOrdenVentaDetalle")
            .WithSummary("Escritura: VENTAS.usp_OrdenVentaDet_Ins. Paridad OrdenVentaDetBL.AgregarOrdenVentaDetalle.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/actualizar-orden-venta-detalle",
                async Task<Results<Ok<OrdenVentaOperacionResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarOrdenVentaDetalleRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaDetScopeReadService ordenVentaDetScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaDetId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaDetId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scope = await ordenVentaDetScope.GetScopeByDetIdAsync(body.OrdenVentaDetId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el detalle de orden de venta indicado.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El detalle no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ActualizarOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaWrite
                            .ActualizarDetalleAsync(body.OrdenVentaDetId, body.Descuento, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVentaDet_update");
                        var detail = "No se pudo actualizar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasActualizarOrdenVentaDetalle")
            .WithSummary("Escritura: VENTAS.usp_OrdenVentaDet_update. Paridad OrdenVentaDetBL.ActualizarOrdenVentaDetalle.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/eliminar-orden-venta-detalle",
                async Task<Results<Ok<OrdenVentaOperacionResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarOrdenVentaDetalleRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaDetScopeReadService ordenVentaDetScope,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaDetId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaDetId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var scope = await ordenVentaDetScope.GetScopeByDetIdAsync(body.OrdenVentaDetId, ct).ConfigureAwait(false);
                    if (scope is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el detalle de orden de venta indicado.");
                    }

                    if (scope.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El detalle no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarOrdenVentaDetalle");
                    try
                    {
                        var response = await ordenVentaWrite
                            .EliminarDetalleAsync(body.OrdenVentaDetId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVenta_Del (detalle)");
                        var detail = "No se pudo eliminar el detalle de la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEliminarOrdenVentaDetalle")
            .WithSummary("Escritura: VENTAS.usp_OrdenVenta_Del(0, detId). Paridad OrdenVentaDetBL.EliminarOrdenVentaDetalle.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/eliminar-orden-venta",
                async Task<Results<Ok<OrdenVentaOperacionResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarOrdenVentaRequest body,
                    IOrdenVentaWriteService ordenVentaWrite,
                    IOrdenVentaOficinaReadService ordenVentaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var ordenOid = await ordenVentaOficina
                        .GetOficinaIdByOrdenVentaIdAsync(body.OrdenVentaId, ct)
                        .ConfigureAwait(false);
                    if (ordenOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (ordenOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarOrdenVenta");
                    try
                    {
                        var response = await ordenVentaWrite.EliminarOrdenAsync(body.OrdenVentaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al ejecutar VENTAS.usp_OrdenVenta_Del");
                        var detail = "No se pudo eliminar la orden de venta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEliminarOrdenVenta")
            .WithSummary("Escritura: VENTAS.usp_OrdenVenta_Del. Paridad OrdenVentaBL.EliminarOrdenVenta.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<OrdenVentaOperacionResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/enviar-orden-venta-contado",
                async Task<Results<Ok<EnviarOrdenVentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EnviarOrdenVentaRequest body,
                    IOrdenVentaEnvioReadService ordenEnvioRead,
                    IOrdenVentaEnvioWriteService ordenEnvioWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var orden = await ordenEnvioRead.GetOrdenAsync(body.OrdenVentaId, ct).ConfigureAwait(false);
                    if (orden is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (orden.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    if (string.Equals(orden.Estado, "ENV", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La orden de venta ya fue enviada.");
                    }

                    var log = loggerFactory.CreateLogger("EnviarOrdenVentaContado");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var response = await ordenEnvioWrite
                            .EnviarContadoAsync(body.OrdenVentaId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al enviar orden de venta contado");
                        var detail = "No se pudo enviar la orden de venta al contado.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEnviarOrdenVentaContado")
            .WithSummary("Escritura EF: paridad OrdenVentaBL.EnviarOrdenVentaContado (Estado ENV, TipoVenta CON).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<EnviarOrdenVentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/enviar-orden-venta-credito",
                async Task<Results<Ok<EnviarOrdenVentaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EnviarOrdenVentaRequest body,
                    IOrdenVentaEnvioReadService ordenEnvioRead,
                    IOrdenVentaEnvioWriteService ordenEnvioWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.OrdenVentaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y ordenVentaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var orden = await ordenEnvioRead.GetOrdenAsync(body.OrdenVentaId, ct).ConfigureAwait(false);
                    if (orden is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe la orden de venta indicada.");
                    }

                    if (orden.OficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La orden de venta no pertenece a la oficina del token JWT.");
                    }

                    if (string.Equals(orden.Estado, "ENV", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La orden de venta ya fue enviada.");
                    }

                    if (await ordenEnvioRead.ExisteCreditoVinculadoAsync(body.OrdenVentaId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "Ya existe un crédito vinculado a esta orden de venta.");
                    }

                    var log = loggerFactory.CreateLogger("EnviarOrdenVentaCredito");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var descripciones = await ordenEnvioRead
                            .ListarDescripcionesDetalleActivasAsync(body.OrdenVentaId, ct)
                            .ConfigureAwait(false);

                        var response = await ordenEnvioWrite
                            .EnviarCreditoAsync(
                                body.OrdenVentaId,
                                body.OficinaId,
                                orden.PersonaId,
                                orden.TotalNeto,
                                descripciones,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
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
                        log.LogError(ex, "Error al enviar orden de venta crédito");
                        var detail = "No se pudo enviar la orden de venta a crédito.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasEnviarOrdenVentaCredito")
            .WithSummary(
                "Escritura EF: paridad OrdenVentaBL.EnviarOrdenVentaCredito (INSERT crédito CRE + orden ENV/CRE).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<EnviarOrdenVentaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/caja-diario-venta-rapida",
                async Task<Results<Ok<CajaDiarioVentaRapidaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IVentaRapidaCajaDiarioReadService cajaDiarioVentaRapida,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "El token no contiene un usuario válido (vendix:usuario_id).");
                    }

                    var log = loggerFactory.CreateLogger("CajaDiarioVentaRapida");
                    try
                    {
                        var caja = await cajaDiarioVentaRapida
                            .ObtenerAbiertaPorUsuarioAsync(usuarioId, oficinaId, ct)
                            .ConfigureAwait(false);
                        if (caja is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No hay caja diario abierta asignada al usuario en esta oficina.");
                        }

                        return TypedResults.Ok(caja);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al leer caja diario venta rápida");
                        var detail = "No se pudo obtener la caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasCajaDiarioVentaRapida")
            .WithSummary(
                "Lectura: paridad VentaRapidaController.Index (caja abierta). usuarioId = JWT; oficinaId = JWT; filtra Caja.OficinaId (restricción adicional frente al MVC).")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaDiarioVentaRapidaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/ventas/articulo-venta-rapida",
                async Task<Results<Ok<ArticuloVentaRapidaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    string codigo,
                    IVentaRapidaReadService ventaRapidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(codigo))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "codigo es obligatorio.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ArticuloVentaRapida");
                    try
                    {
                        var articulo = await ventaRapidaRead.ObtenerPorCodigoAsync(codigo, ct).ConfigureAwait(false);
                        if (articulo is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe un artículo activo con ese código y lista de precios.");
                        }

                        return TypedResults.Ok(articulo);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al buscar artículo venta rápida");
                        var detail = "No se pudo obtener el artículo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasArticuloVentaRapida")
            .WithSummary(
                "Lectura: paridad VentaRapidaController.ObtenerArticulo. codigo = CodArticulo; stock = series EN_ALMACEN (EstadoId 2). oficinaId = JWT.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ArticuloVentaRapidaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/ventas/realizar-pedido",
                async Task<Results<Ok<RealizarPedidoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RealizarPedidoRequest body,
                    IVentaRapidaWriteService ventaRapida,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyCajaIds(body.OficinaId, body.CajaDiarioId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.PersonaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "personaId debe ser >= 1.");
                    }

                    if (body.Pedidos is null || body.Pedidos.Count == 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "pedidos debe contener al menos una línea.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaOficinaAsync(body.OficinaId, body.CajaDiarioId, cajaDiarioOficina, ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    var log = loggerFactory.CreateLogger("RealizarPedido");
                    try
                    {
                        if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Conflicto",
                                detail: "La caja diario está cerrada.");
                        }

                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                        if (serverTime is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Error de base de datos",
                                detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                        }

                        var (error, result) = await ventaRapida
                            .RealizarPedidoAsync(
                                body.OficinaId,
                                body.CajaDiarioId,
                                body.PersonaId,
                                usuarioId,
                                serverTime.Value,
                                body.Pedidos,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "Venta no completada",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogWarning(ex, "Parámetros inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Los parámetros enviados no son válidos.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al realizar pedido venta rápida");
                        var detail = "No se pudo completar la venta rápida.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("VentasRealizarPedido")
            .WithSummary(
                "Escritura: paridad OrdenVentaBL.RealizarPedido / VentaRapidaController. Orden CON+ENV, detalle, series y usp_PagarCuentaxCobrar en una transacción.")
            .WithTags("ventas")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RealizarPedidoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    }
}
