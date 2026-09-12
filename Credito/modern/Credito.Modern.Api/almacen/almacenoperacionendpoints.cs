using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Hosting;
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

namespace Credito.Modern.Api.Almacen;

internal static class AlmacenOperacionEndpoints
{
    public static void MapAlmacenOperacionEndpoints(this WebApplication app)
    {

        app.MapGet(
                "/api/v1/almacen/rpt-stock-anulados",
                async Task<Results<Ok<List<RptStockAnuladoRowDto>>, ProblemHttpResult>> (
                    IRptStockAnuladosReadService stockAnulados,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("RptStockAnulados");
                    try
                    {
                        var items = await stockAnulados.ListarAsync(ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al consultar stock anulado");
                        var detail = "No se pudo obtener el reporte de stock anulado.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenRptStockAnulados")
            .WithSummary(
                "Solo lectura: consulta equivalente a ReporteBL.ListarReporteStockAnulados (movimientos anulados sin filtro de oficina). CreditoUser.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<RptStockAnuladoRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/rpt-stock-anulados-csv",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    IRptStockAnuladosReadService stockAnulados,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("RptStockAnuladosCsv");
                    try
                    {
                        var items = await stockAnulados.ListarAsync(ct).ConfigureAwait(false);
                        var bytes = RptStockAnuladosCsvFormatter.ToUtf8BomCsv(items);
                        return TypedResults.File(bytes, "text/csv; charset=utf-8", fileDownloadName: "stock-anulados.csv");
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
                        log.LogError(ex, "Error al consultar stock anulado (CSV)");
                        var detail = "No se pudo generar el CSV de stock anulado.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenRptStockAnuladosCsv")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-stock-anulados en CSV UTF-8 (BOM). ReporteBL.ListarReporteStockAnulados sin motor RDLC. CreditoUser.")
            .WithTags("almacen", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/rpt-stock-anulados-pdf",
                async Task<Results<FileContentHttpResult, ProblemHttpResult>> (
                    IRptStockAnuladosReadService stockAnulados,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("RptStockAnuladosPdf");
                    try
                    {
                        var items = await stockAnulados.ListarAsync(ct).ConfigureAwait(false);
                        var csvBytes = RptStockAnuladosCsvFormatter.ToUtf8BomCsv(items);
                        var bytes = TabularPdfDocument.FromUtf8BomCsv("Stock anulados", csvBytes);
                        return TypedResults.File(bytes, "application/pdf", fileDownloadName: "stock-anulados.pdf");
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
                        log.LogError(ex, "Error al consultar stock anulado (PDF)");
                        var detail = "No se pudo generar el PDF de stock anulado.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenRptStockAnuladosPdf")
            .WithSummary(
                "Export lectura: mismos datos que GET rpt-stock-anulados en PDF tabular (QuestPDF). ReporteBL.ListarReporteStockAnulados sin motor RDLC. CreditoUser.")
            .WithTags("almacen", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/movimientos-entrada",
                async Task<Results<Ok<MovimientoEntradaListPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int almacenId,
                    string? buscar,
                    int articuloId,
                    int page,
                    int pageSize,
                    IMovimientoEntradaReadService entradaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || almacenId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y almacenId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    if (!await entradaRead.AlmacenPerteneceOficinaAsync(almacenId, oficinaId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El almacén no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("MovimientosEntrada");
                    try
                    {
                        var response = await entradaRead
                            .ListarEntradasAsync(
                                oficinaId,
                                almacenId,
                                buscar,
                                articuloId,
                                page < 1 ? 1 : page,
                                pageSize < 1 ? 25 : pageSize,
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
                        log.LogError(ex, "Error al listar entradas de almacén");
                        var detail = "No se pudo listar movimientos de entrada.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenMovimientosEntrada")
            .WithSummary("Paridad EntradaController.Listar / MovimientoBL.LstEntradaJGrid.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoEntradaListPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/movimiento-entrada/{movimientoId:int}",
                async Task<Results<Ok<MovimientoEntradaDetalleResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int movimientoId,
                    int oficinaId,
                    IMovimientoEntradaReadService entradaRead,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1 || movimientoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y movimientoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(movimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento de almacén indicado.");
                    }

                    if (movOid.Value != oficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("MovimientoEntradaDetalle");
                    try
                    {
                        var response = await entradaRead.ObtenerEntradaAsync(movimientoId, ct).ConfigureAwait(false);
                        if (response is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe el movimiento de entrada indicado.");
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
                        log.LogError(ex, "Error al obtener movimiento de entrada");
                        var detail = "No se pudo obtener el movimiento de entrada.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenMovimientoEntradaDetalle")
            .WithSummary("Paridad EntradaController.ObtenerMovimiento + ListarDocs + ListarDetalle.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoEntradaDetalleResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/crear-movimiento",
                async Task<Results<Ok<CrearMovimientoResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearMovimientoRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoEntradaReadService entradaRead,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.AlmacenId < 1 || body.TipoMovimientoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, almacenId y tipoMovimientoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    if (!await entradaRead.AlmacenPerteneceOficinaAsync(body.AlmacenId, body.OficinaId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El almacén no pertenece a la oficina del token JWT.");
                    }

                    if (!await entradaRead.TipoMovimientoEsEntradaAsync(body.TipoMovimientoId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Tipo inválido",
                            detail: "El tipo de movimiento debe ser de entrada (IndEntrada).");
                    }

                    var log = loggerFactory.CreateLogger("CrearMovimiento");
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

                        var response = await movimientoWrite
                            .CrearMovimientoAsync(body.AlmacenId, body.TipoMovimientoId, serverTime.Value, ct)
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
                        log.LogError(ex, "Error al crear movimiento de almacén");
                        var detail = "No se pudo crear el movimiento de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenCrearMovimiento")
            .WithSummary("Paridad EntradaController.Guardar (pMovimientoId=0).")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CrearMovimientoResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/agregar-movimiento-documento",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AgregarMovimientoDocumentoRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoId < 1 || body.TipoDocumentoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, movimientoId y tipoDocumentoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(body.MovimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento indicado.");
                    }

                    if (movOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("AgregarMovimientoDocumento");
                    try
                    {
                        await movimientoWrite
                            .AgregarDocumentoAsync(
                                body.MovimientoId,
                                body.TipoDocumentoId,
                                body.SerieDocumento,
                                body.NroDocumento,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new MovimientoOperacionMensajeResponse(string.Empty));
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("pendiente", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al agregar documento de movimiento");
                        var detail = "No se pudo agregar el documento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenAgregarMovimientoDocumento")
            .WithSummary("Paridad EntradaController.AgregarDocumento.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/eliminar-movimiento-documento",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarMovimientoDocumentoRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoDocId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y movimientoDocId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("EliminarMovimientoDocumento");
                    try
                    {
                        await movimientoWrite.EliminarDocumentoAsync(body.MovimientoDocId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new MovimientoOperacionMensajeResponse(string.Empty));
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("pendiente", StringComparison.OrdinalIgnoreCase)
                                                               || ex.Message.Contains("eliminar", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al eliminar documento de movimiento");
                        var detail = "No se pudo eliminar el documento.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenEliminarMovimientoDocumento")
            .WithSummary("Paridad EntradaController.Eliminar (documento).")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/actualizar-importe-movimiento",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarImporteMovimientoRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y movimientoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(body.MovimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento indicado.");
                    }

                    if (movOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ActualizarImporteMovimiento");
                    try
                    {
                        await movimientoWrite
                            .ActualizarImporteAsync(body.MovimientoId, body.AjusteRedondeo, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new MovimientoOperacionMensajeResponse(string.Empty));
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("pendiente", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al actualizar importe del movimiento");
                        var detail = "No se pudo actualizar el importe.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenActualizarImporteMovimiento")
            .WithSummary("Paridad MovimientoBL.ActualizarImporte.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/buscar-serie-salida",
                async Task<Results<Ok<BuscarSerieSalidaResponse>, ProblemHttpResult>> (
                    string numeroSerie,
                    ISalidaAlmacenReadService salidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(numeroSerie))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "numeroSerie es obligatorio.");
                    }

                    var log = loggerFactory.CreateLogger("BuscarSerieSalida");
                    try
                    {
                        var response = await salidaRead.BuscarSerieAsync(numeroSerie, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al buscar serie para salida");
                        var detail = "No se pudo buscar la serie.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenBuscarSerieSalida")
            .WithSummary("Paridad SalidaController.BuscarProducto (serie EN ALMACEN, EstadoId=2).")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BuscarSerieSalidaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/realizar-salida",
                async Task<Results<Ok<RealizarSalidaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RealizarSalidaRequest body,
                    ISalidaAlmacenWriteService salidaWrite,
                    ISalidaAlmacenReadService salidaRead,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TipoMovimientoId < 1 || body.Series.Count == 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, tipoMovimientoId y al menos una serie son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    if (!await salidaRead.TipoMovimientoEsSalidaAsync(body.TipoMovimientoId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Tipo inválido",
                            detail: "El tipo de movimiento no es válido para salida de almacén.");
                    }

                    var almacenId = await salidaRead
                        .GetAlmacenPredeterminadoOficinaAsync(body.OficinaId, ct)
                        .ConfigureAwait(false);
                    if (almacenId is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No hay almacén activo para la oficina.");
                    }

                    var log = loggerFactory.CreateLogger("RealizarSalida");
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

                        var response = await salidaWrite
                            .RealizarSalidaAsync(
                                body.OficinaId,
                                almacenId.Value,
                                body.TipoMovimientoId,
                                body.Glosa ?? string.Empty,
                                body.Series,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex) when (
                        ex.Message.Contains("serie", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("almacén", StringComparison.OrdinalIgnoreCase))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.");
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
                        log.LogError(ex, "Error al realizar salida de almacén");
                        var detail = "No se pudo registrar la salida de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenRealizarSalida")
            .WithSummary("Paridad SalidaController.RealizarSalida (movimiento EstadoId=3, series anuladas).")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<RealizarSalidaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/transferencias",
                async Task<Results<Ok<TransferenciaListPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    string? buscar,
                    int almacenId,
                    int articuloId,
                    int page,
                    int pageSize,
                    ITransferenciaReadService transferenciaRead,
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

                    var log = loggerFactory.CreateLogger("TransferenciasListar");
                    try
                    {
                        var response = await transferenciaRead
                            .ListarAsync(
                                oficinaId,
                                buscar,
                                almacenId,
                                articuloId,
                                page < 1 ? 1 : page,
                                pageSize < 1 ? 25 : pageSize,
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
                        log.LogError(ex, "Error al listar transferencias");
                        var detail = "No se pudo listar transferencias.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenTransferenciasListar")
            .WithSummary("Paridad TransferenciaController.Listar / TransferenciaBL.LstTransferenciaJGrid.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TransferenciaListPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/transferencia/{transferenciaId:int}",
                async Task<Results<Ok<TransferenciaCabeceraDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int transferenciaId,
                    int oficinaId,
                    ITransferenciaReadService transferenciaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (transferenciaId < 1 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "transferenciaId y oficinaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("TransferenciaCabecera");
                    try
                    {
                        var cabecera = await transferenciaRead
                            .ObtenerCabeceraAsync(transferenciaId, oficinaId, ct)
                            .ConfigureAwait(false);
                        if (cabecera is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Transferencia no encontrada o sin acceso.");
                        }

                        return TypedResults.Ok(cabecera);
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
                        log.LogError(ex, "Error al obtener transferencia");
                        var detail = "No se pudo obtener la transferencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenTransferenciaObtener")
            .WithSummary("Paridad TransferenciaController.ObtenerMovimientoExt.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TransferenciaCabeceraDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/almacen/transferencia/{transferenciaId:int}/detalle",
                async Task<Results<Ok<List<TransferenciaDetalleLineaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int transferenciaId,
                    int oficinaId,
                    ITransferenciaReadService transferenciaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (transferenciaId < 1 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "transferenciaId y oficinaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("TransferenciaDetalle");
                    try
                    {
                        var lineas = await transferenciaRead
                            .ListarDetalleAsync(transferenciaId, oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(lineas.ToList());
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
                        log.LogError(ex, "Error al listar detalle de transferencia");
                        var detail = "No se pudo listar el detalle.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenTransferenciaDetalle")
            .WithSummary("Paridad detalle agrupado por artículo (series en transferencia).")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<TransferenciaDetalleLineaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/crear-transferencia",
                async Task<Results<Ok<CrearTransferenciaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearTransferenciaRequest body,
                    ITransferenciaWriteService transferenciaWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.AlmacenDestinoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y almacenDestinoId son obligatorios.");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId) || usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "No autorizado",
                            detail: "Token sin usuario válido.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("CrearTransferencia");
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

                        var response = await transferenciaWrite
                            .CrearAsync(body.OficinaId, usuarioId, body.AlmacenDestinoId, serverTime.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase))
                    {
                        log.LogWarning(ex, "Cadena de conexión no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Crear transferencia");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "La solicitud enviada no es válida.");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al crear transferencia");
                        var detail = "No se pudo crear la transferencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenCrearTransferencia")
            .WithSummary("Paridad TransferenciaController.CrearTransferencia.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CrearTransferenciaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/validar-serie-transferencia",
                async Task<Results<Ok<ValidarSerieTransferenciaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ValidarSerieTransferenciaRequest body,
                    ITransferenciaWriteService transferenciaWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TransferenciaId < 1 || string.IsNullOrWhiteSpace(body.NumeroSerie))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, transferenciaId y numeroSerie son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarSerieTransferencia");
                    try
                    {
                        var response = await transferenciaWrite
                            .ValidarYAgregarSerieAsync(
                                body.OficinaId,
                                body.TransferenciaId,
                                body.NumeroSerie,
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
                        log.LogError(ex, "Error al validar serie transferencia");
                        var detail = "No se pudo validar la serie.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenValidarSerieTransferencia")
            .WithSummary("Paridad TransferenciaController.ValidarSerie.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarSerieTransferenciaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/eliminar-serie-transferencia",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarSerieTransferenciaRequest body,
                    ITransferenciaWriteService transferenciaWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TransferenciaId < 1 || body.ArticuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, transferenciaId y articuloId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("EliminarSerieTransferencia");
                    try
                    {
                        var ok = await transferenciaWrite
                            .EliminarSeriesPorArticuloAsync(
                                body.OficinaId,
                                body.TransferenciaId,
                                body.ArticuloId,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(ok);
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
                        log.LogError(ex, "Error al eliminar series transferencia");
                        var detail = "No se pudo eliminar las series.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenEliminarSerieTransferencia")
            .WithSummary("Paridad TransferenciaController.EliminarTransferenciaSerie.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/desconfirmar-transferencia",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferenciaOperacionRequest body,
                    ITransferenciaWriteService transferenciaWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TransferenciaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y transferenciaId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("DesconfirmarTransferencia");
                    try
                    {
                        var ok = await transferenciaWrite
                            .DesconfirmarAsync(body.OficinaId, body.TransferenciaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(ok);
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
                        log.LogError(ex, "Error al desconfirmar transferencia");
                        var detail = "No se pudo desconfirmar.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenDesconfirmarTransferencia")
            .WithSummary("Paridad TransferenciaController.Desconfirmar.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<bool>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/confirmar-transferencia",
                async Task<Results<Ok<ConfirmarTransferenciaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferenciaOperacionRequest body,
                    ITransferenciaWriteService transferenciaWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.TransferenciaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y transferenciaId son obligatorios.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ConfirmarTransferencia");
                    try
                    {
                        var response = await transferenciaWrite
                            .ConfirmarAsync(body.OficinaId, body.TransferenciaId, ct)
                            .ConfigureAwait(false);
                        if (!response.Success)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "No confirmada",
                                detail: response.Mensaje);
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
                        log.LogError(ex, "Error al confirmar transferencia");
                        var detail = "No se pudo confirmar la transferencia.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenConfirmarTransferencia")
            .WithSummary("Paridad TransferenciaController.Confirmar / TransferenciaBL.TransferirSeries.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ConfirmarTransferenciaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/confirmar-movimiento",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    MovimientoOperacionRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y movimientoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(body.MovimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento de almacén indicado.");
                    }

                    if (movOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ConfirmarMovimiento");
                    try
                    {
                        var response = await movimientoWrite.ConfirmarAsync(body.MovimientoId, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_Movimiento_Upd (confirmar)");
                        var detail = "No se pudo confirmar el movimiento de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenConfirmarMovimiento")
            .WithSummary("Escritura: ALMACEN.usp_Movimiento_Upd flag=3. Paridad MovimientoBL.ConfirmarMov.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/desconfirmar-movimiento",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    MovimientoOperacionRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y movimientoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(body.MovimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento de almacén indicado.");
                    }

                    if (movOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("DesconfirmarMovimiento");
                    try
                    {
                        var response = await movimientoWrite.DesconfirmarAsync(body.MovimientoId, ct).ConfigureAwait(false);
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
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_Movimiento_Upd (desconfirmar)");
                        var detail = "No se pudo desconfirmar el movimiento de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenDesconfirmarMovimiento")
            .WithSummary("Escritura: ALMACEN.usp_Movimiento_Upd flag=1. Paridad MovimientoBL.Desconfirmar.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/actualizar-movimiento",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarMovimientoRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoId < 1 || body.TipoMovimientoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, movimientoId y tipoMovimientoId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(body.MovimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento de almacén indicado.");
                    }

                    if (movOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("ActualizarMovimiento");
                    try
                    {
                        await movimientoWrite
                            .ActualizarAsync(
                                body.MovimientoId,
                                body.TipoMovimientoId,
                                body.Fecha,
                                body.Observacion ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new MovimientoOperacionMensajeResponse(string.Empty));
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
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_Movimiento_Upd (actualizar)");
                        var detail = "No se pudo actualizar el movimiento de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenActualizarMovimiento")
            .WithSummary("Escritura: ALMACEN.usp_Movimiento_Upd flag=2. Paridad MovimientoBL.ActualizarMov.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/crear-movimiento-detalle",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearMovimientoDetalleRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoOficinaReadService movimientoOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoId < 1 || body.ArticuloId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, movimientoId y articuloId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var movOid = await movimientoOficina.GetOficinaIdByMovimientoIdAsync(body.MovimientoId, ct).ConfigureAwait(false);
                    if (movOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el movimiento de almacén indicado.");
                    }

                    if (movOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El movimiento no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("CrearMovimientoDetalle");
                    try
                    {
                        await movimientoWrite
                            .CrearDetalleAsync(
                                body.MovimientoId,
                                body.MovimientoDetId,
                                body.ArticuloId,
                                body.IndAutogenerar,
                                body.ListaSerie ?? string.Empty,
                                body.Cantidad,
                                body.IndCorrelativo,
                                body.PrecioUnitario,
                                body.Descuento,
                                body.Medida,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new MovimientoOperacionMensajeResponse(string.Empty));
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
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_CrearMovimientoDet");
                        var detail = "No se pudo crear el detalle del movimiento de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenCrearMovimientoDetalle")
            .WithSummary("Escritura: ALMACEN.usp_CrearMovimientoDet. Paridad MovimientoDetBL.CrearDetalle.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/almacen/eliminar-movimiento-detalle",
                async Task<Results<Ok<MovimientoOperacionMensajeResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EliminarMovimientoDetalleRequest body,
                    IMovimientoAlmacenWriteService movimientoWrite,
                    IMovimientoDetOficinaReadService movimientoDetOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.MovimientoDetId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId y movimientoDetId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var detOid = await movimientoDetOficina
                        .GetOficinaIdByMovimientoDetIdAsync(body.MovimientoDetId, ct)
                        .ConfigureAwait(false);
                    if (detOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe el detalle de movimiento indicado.");
                    }

                    if (detOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El detalle no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("EliminarMovimientoDetalle");
                    try
                    {
                        await movimientoWrite.EliminarDetalleAsync(body.MovimientoDetId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(new MovimientoOperacionMensajeResponse(string.Empty));
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
                        log.LogError(ex, "Error al ejecutar ALMACEN.usp_EliminarMovimientoDet");
                        var detail = "No se pudo eliminar el detalle del movimiento de almacén.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("AlmacenEliminarMovimientoDetalle")
            .WithSummary("Escritura: ALMACEN.usp_EliminarMovimientoDet. Paridad MovimientoDetBL.EliminarDetalle.")
            .WithTags("almacen")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<MovimientoOperacionMensajeResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);


    }
}
