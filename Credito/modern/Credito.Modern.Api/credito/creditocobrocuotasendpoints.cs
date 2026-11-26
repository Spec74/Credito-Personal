using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Validation;
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
using Credito.Modern.Application.MediosPago;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Productos;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.Time;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.Ventas;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Credito;

/// <summary>Cobro de cuotas, verificación de transferencias e impagos.</summary>
internal static class CreditoCobroCuotasEndpoints
{
    public static void MapCreditoCobroCuotasEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/pagos-no-verificados",
                async Task<Results<Ok<List<PagosNoVerificadosRowDto>>, ProblemHttpResult>> (
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
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina v?lida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("PagosNoVerificados");
                    try
                    {
                        var items = await pagosNoVerificados.ListarAsync(oficinaId.Value, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagosNoVerificados");
                        var detail = "No se pudo obtener el listado de pagos no verificados.";
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
            .WithName("CreditoPagosNoVerificados")
            .WithSummary(
                "Solo lectura: CREDITO.usp_PagosNoVerificados() con post-filtro por oficina JWT.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<PagosNoVerificadosRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/verificar-pago-transferencia",
                async Task<Results<Ok<VerificarPagoTransferenciaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    VerificarPagoTransferenciaRequest body,
                    IMovimientoCajaScopeReadService movimientoScope,
                    IVerificarPagoTransferenciaWriteService verificarPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyMovimientoIds(body.OficinaId, body.MovimientoCajaId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("VerificarPagoTransferencia");
                    try
                    {
                        var (scopeError, scope) = await CajaCreditoWriteGuards
                            .ValidateMovimientoOficinaAsync(body.OficinaId, body.MovimientoCajaId, movimientoScope, ct)
                            .ConfigureAwait(false);
                        if (scopeError is not null)
                        {
                            return scopeError;
                        }

                        if (scope!.CajaDiarioCerrada)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status409Conflict,
                                title: "Caja cerrada",
                                detail: "No se puede verificar un pago en una caja diario cerrada.");
                        }

                        var response = await verificarPago
                            .VerificarAsync(body.MovimientoCajaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Verificar pago transferencia");
                        var status = ex.Message.Contains("No existe extensi?n", StringComparison.OrdinalIgnoreCase)
                            ? StatusCodes.Status404NotFound
                            : StatusCodes.Status503ServiceUnavailable;
                        return TypedResults.Problem(
                            statusCode: status,
                            title: status == StatusCodes.Status404NotFound ? "No encontrado" : "Error de operaci?n",
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al verificar pago por transferencia");
                        var detail = "No se pudo verificar el pago.";
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
            .WithName("CreditoVerificarPagoTransferencia")
            .WithSummary(
                "Escritura: marca IndTransferenciaVerificada en MovimientoCajaExtension (paridad VerificarPagosController.Verificar). oficinaId = JWT.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<VerificarPagoTransferenciaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/credito/conciliar-pagos-yape",
                async Task<Results<Ok<MedioDigitalConciliacionResultadoDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    HttpRequest httpRequest,
                    IMediosDigitalesConciliacionService conciliacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Se espera multipart/form-data con el archivo del reporte Yape.");
                    }

                    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
                    var formError = TryReadOficinaYAplicar(form, httpContext, out var oficinaId, out var aplicarAutos);
                    if (formError is not null)
                    {
                        return formError;
                    }

                    var file = form.Files.GetFile("archivo") ?? form.Files.GetFile("file");
                    if (file is null || file.Length < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Archivo requerido",
                            detail: "Suba el reporte Yape (.xlsx o .csv).");
                    }

                    if (file.Length > 8 * 1024 * 1024)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Archivo demasiado grande",
                            detail: "El reporte Yape no debe superar 8 MB.");
                    }

                    var log = loggerFactory.CreateLogger("ConciliarPagosYape");
                    try
                    {
                        await using var stream = file.OpenReadStream();
                        var result = await conciliacion
                            .ConciliarYapeAsync(oficinaId, stream, file.FileName, aplicarAutos, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Conciliación Yape rechazada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Reporte no válido",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se pudo interpretar el reporte Yape."
                                : ex.Message);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos conciliación Yape");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error de BD en conciliación Yape");
                        var detail = "No se pudo conciliar el reporte Yape.";
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
            .WithName("CreditoConciliarPagosYape")
            .WithSummary(
                "Encargado/Admin: sube reporte Yape (TE PAGÓ), concilia 1:1 por monto+minuto y opcionalmente auto-verifica solo coincidencias únicas.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<MedioDigitalConciliacionResultadoDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/credito/conciliar-pagos-plin",
                async Task<Results<Ok<MedioDigitalConciliacionResultadoDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    HttpRequest httpRequest,
                    IMediosDigitalesConciliacionService conciliacion,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Se espera multipart/form-data con capturas Plin/Interbank.");
                    }

                    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
                    var formError = TryReadOficinaYAplicar(form, httpContext, out var oficinaId, out var aplicarAutos);
                    if (formError is not null)
                    {
                        return formError;
                    }

                    var files = form.Files
                        .Where(f =>
                            string.Equals(f.Name, "capturas", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(f.Name, "archivo", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(f.Name, "files", StringComparison.OrdinalIgnoreCase) ||
                            f.Name.StartsWith("capturas", StringComparison.OrdinalIgnoreCase))
                        .Where(f => f.Length > 0)
                        .ToList();

                    if (files.Count == 0)
                    {
                        files = form.Files.Where(f => f.Length > 0).ToList();
                    }

                    if (files.Count == 0)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Capturas requeridas",
                            detail: "Suba una o más capturas de movimientos Plin (jpg/png/webp).");
                    }

                    if (files.Any(f => f.Length > 6 * 1024 * 1024))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Archivo demasiado grande",
                            detail: "Cada captura no debe superar 6 MB.");
                    }

                    var log = loggerFactory.CreateLogger("ConciliarPagosPlin");
                    var opened = new List<(Stream Stream, string FileName, IDisposable? Own)>();
                    try
                    {
                        foreach (var f in files)
                        {
                            opened.Add((f.OpenReadStream(), f.FileName, null));
                        }

                        var result = await conciliacion
                            .ConciliarPlinCapturasAsync(
                                oficinaId,
                                opened.Select(x => (x.Stream, x.FileName)).ToList(),
                                aplicarAutos,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Conciliación Plin rechazada");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Capturas no válidas",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se pudieron interpretar las capturas Plin."
                                : ex.Message);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos conciliación Plin");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error de BD en conciliación Plin");
                        var detail = "No se pudo conciliar las capturas Plin.";
                        if (env.IsDevelopment())
                        {
                            detail += $" Detalle: {ex.Message}";
                        }

                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                    finally
                    {
                        foreach (var (stream, _, _) in opened)
                        {
                            await stream.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                })
            .WithName("CreditoConciliarPagosPlin")
            .WithSummary(
                "Encargado/Admin: sube capturas Plin/Interbank, OCR → filas Plin-, concilia 1:1 y opcionalmente auto-verifica únicos.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<MedioDigitalConciliacionResultadoDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/credito/validar-voucher-captura",
                async Task<Results<Ok<VoucherValidacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    HttpRequest httpRequest,
                    IVoucherValidacionService voucherValidacion,
                    ILoggerFactory loggerFactory,
                    CancellationToken ct) =>
                {
                    if (!httpRequest.HasFormContentType)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "Se espera multipart/form-data con la foto de la constancia.");
                    }

                    var form = await httpRequest.ReadFormAsync(ct).ConfigureAwait(false);
                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina válida.");
                    }

                    var oficinaRaw = form["oficinaId"].ToString();
                    if (!int.TryParse(oficinaRaw, out var oficinaId) || oficinaId < 1)
                    {
                        oficinaId = jwtOficinaId;
                    }

                    if (oficinaId != jwtOficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con el JWT.");
                    }

                    decimal? montoEsperado = null;
                    var montoRaw = form["montoEsperado"].ToString();
                    if (!string.IsNullOrWhiteSpace(montoRaw) &&
                        decimal.TryParse(
                            montoRaw.Replace(',', '.'),
                            System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var montoParsed) &&
                        montoParsed > 0)
                    {
                        montoEsperado = decimal.Round(montoParsed, 2, MidpointRounding.AwayFromZero);
                    }

                    var file = form.Files.FirstOrDefault(f => f.Length > 0);
                    if (file is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Captura requerida",
                            detail: "Tome una foto o suba la constancia (jpg/png/webp).");
                    }

                    if (file.Length > 8 * 1024 * 1024)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Archivo demasiado grande",
                            detail: "La captura no debe superar 8 MB.");
                    }

                    var contentType = (file.ContentType ?? "").ToLowerInvariant();
                    var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
                    var mimeOk =
                        contentType is "" or "application/octet-stream" ||
                        contentType.StartsWith("image/", StringComparison.Ordinal);
                    var extOk = ext is "" or ".jpg" or ".jpeg" or ".png" or ".webp" or ".jfif" or ".heic";
                    if (!mimeOk || !extOk)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Formato no soportado",
                            detail: "Use foto JPG, PNG o WEBP de la constancia.");
                    }

                    var canalEsperado = form["canalEsperado"].ToString();
                    if (string.IsNullOrWhiteSpace(canalEsperado))
                    {
                        canalEsperado = form["marcaCodigo"].ToString();
                    }

                    var log = loggerFactory.CreateLogger("ValidarVoucherCaptura");
                    try
                    {
                        await using var stream = file.OpenReadStream();
                        var result = await voucherValidacion
                            .ValidarCapturaAsync(
                                stream,
                                file.FileName,
                                montoEsperado,
                                string.IsNullOrWhiteSpace(canalEsperado) ? null : canalEsperado.Trim(),
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "OCR voucher no disponible o rechazado");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "OCR no disponible",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se pudo leer la constancia."
                                : ex.Message);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error al validar voucher");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Error",
                            detail: "No se pudo procesar la captura del voucher.");
                    }
                })
            .WithName("CreditoValidarVoucherCaptura")
            .WithSummary(
                "Caja: foto de constancia (Yape/Plin/BCP/Interbank/Nación) → OCR → monto, fecha/hora y nro. operación.")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<VoucherValidacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/completar-impagos-validacion",
                async Task<Results<Ok<CompletarImpagosValidacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int? oficinaId,
                    int? cajaDiarioId,
                    ICompletarImpagosValidacionReadService completarImpagosValidacion,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId is null or < 1 || cajaDiarioId is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: "oficinaId y cajaDiarioId son obligatorios y deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina v?lida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var cajaOid = await cajaDiarioOficina
                        .GetOficinaIdByCajaDiarioIdAsync(cajaDiarioId.Value, ct)
                        .ConfigureAwait(false);
                    if (cajaOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una caja diario con el cajaDiarioId indicado.");
                    }

                    if (cajaOid.Value != oficinaId.Value)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La caja diario no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("CompletarImpagosValidacion");
                    try
                    {
                        var response = await completarImpagosValidacion
                            .ValidarAsync(cajaDiarioId.Value, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "cajaDiarioId inv?lido");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CompletarImpagosValidacion");
                        var detail = "No se pudo validar impagos pendientes de la caja diario.";
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
            .WithName("CreditoCompletarImpagosValidacion")
            .WithSummary(
                "Solo lectura: CREDITO.usp_CompletarImpagosValidacion(CajaDiarioId). cajaDiarioId y oficinaId obligatorios; oficinaId = vendix:oficina_id y la caja diario debe pertenecer a esa oficina. cantidadImpagosPendientes > 0 bloquea flujos en MVC (CreditoBL.CompletarImpagosValidar / CreditoController.ValidarCredito). CreditoUser. Distinto de POST completar-impagos (usp_CompletarImpagos, escritura).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CompletarImpagosValidacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/completar-impagos",
                async Task<Results<Ok<CompletarImpagosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CompletarImpagosRequest body,
                    ICompletarImpagosWriteService completarImpagos,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1 || body.CajaDiarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId y cajaDiarioId deben ser enteros >= 1.");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token no contiene una oficina v?lida (vendix:oficina_id).");
                    }

                    if (jwtOficinaId != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "oficinaId debe coincidir con la oficina del token JWT.");
                    }

                    var cajaOid = await cajaDiarioOficina
                        .GetOficinaIdByCajaDiarioIdAsync(body.CajaDiarioId, ct)
                        .ConfigureAwait(false);
                    if (cajaOid is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "No encontrado",
                            detail: "No existe una caja diario con el cajaDiarioId indicado.");
                    }

                    if (cajaOid.Value != body.OficinaId)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "La caja diario no pertenece a la oficina del token JWT.");
                    }

                    var log = loggerFactory.CreateLogger("CompletarImpagos");
                    try
                    {
                        var response = await completarImpagos.EjecutarAsync(body.CajaDiarioId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_CompletarImpagos");
                        var detail = "No se pudo completar impagos de la caja diario.";
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
            .WithName("CreditoCompletarImpagos")
            .WithSummary(
                "Escritura: CREDITO.usp_CompletarImpagos(CajaDiarioId). Body { oficinaId, cajaDiarioId }; oficinaId = vendix:oficina_id. Paridad CreditoBL.CompletarImpagos (el SP registra CUO 0 en cr?ditos sin cobro del d?a). GET completar-impagos-validacion se usa en cierre, no como rechazo de esta escritura.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<CompletarImpagosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuotas",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotasRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaPagoMoraOrchestrator moraOrchestrator,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IDatabaseTimeProvider databaseTime,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (string.IsNullOrWhiteSpace(body.ListaPlanPagoId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "listaPlanPagoId es obligatorio.");
                    }

                    var moneyError = ProblemResults.IfInvalid(
                        MoneyRules.RequirePositive(body.ImporteRecibido, "importeRecibido"));
                    if (moneyError is not null)
                        return moneyError;

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var (sesionTipo, sesionTipoErr) = await SesionOficina
                        .ResolverAsync(httpContext, oficinas, null, ct)
                        .ConfigureAwait(false);
                    if (sesionTipoErr is not null)
                        return sesionTipoErr;

                    var tipoPagoError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(body.TipoPagoId, sesionTipo!.EsCentral)
                        ?? TipoPagoRules.RequireDigitalFecha(
                            body.TipoPagoId,
                            body.FechaPagoTransferencia)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(body.OficinaId, body.TipoPagoId, ct)
                            .ConfigureAwait(false));
                    if (tipoPagoError is not null)
                        return tipoPagoError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario est? cerrada.");
                    }

                    var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                    if (serverTime is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos",
                            detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotas");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotasAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ListaPlanPagoId,
                                body.ImporteRecibido,
                                usuarioId,
                                serverTime.Value,
                                body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

                        if (body.AplicarMoraPostergada && response.ResultId is > 0)
                        {
                            try
                            {
                                var planIds = body.ListaPlanPagoId
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Select(s => int.TryParse(s, out var id) ? id : 0)
                                    .Where(id => id > 0)
                                    .ToList();
                                await moraOrchestrator
                                    .AplicarTrasPagoCuotasAsync(
                                        body.CreditoId,
                                        body.CajaDiarioId,
                                        usuarioId,
                                        body.TipoPagoId,
                                        planIds,
                                        body.EsUltimaCuota,
                                        ct)
                                    .ConfigureAwait(false);
                            }
                            catch (DbException ex)
                            {
                                // El pago ya est? confirmado; no devolver error de cobro por falla de mora postergada.
                                log.LogError(
                                    ex,
                                    "Pago {ResultId} OK; fall? mora postergada (?falta usp_CreditoMora_Registrar/Liquidar en BD?)",
                                    response.ResultId);
                            }
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuotas");
                        var detail = "No se pudo registrar el pago de cuotas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuotas")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuotas. usuarioId y fechaPago desde JWT y usp_FechaBD. Paridad CajaDiarioBL.PagarCuotas / CreditoController.PagarCuotas.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuota-importe-libre",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotaPagoLibreRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var moneyError = ProblemResults.IfInvalid(
                        MoneyRules.RequirePositive(body.ImporteRecibido, "importeRecibido"));
                    if (moneyError is not null)
                        return moneyError;

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var (sesionTipo, sesionTipoErr) = await SesionOficina
                        .ResolverAsync(httpContext, oficinas, null, ct)
                        .ConfigureAwait(false);
                    if (sesionTipoErr is not null)
                        return sesionTipoErr;

                    var tipoPagoError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(body.TipoPagoId, sesionTipo!.EsCentral)
                        ?? TipoPagoRules.RequireDigitalFecha(
                            body.TipoPagoId,
                            body.FechaPagoTransferencia)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(body.OficinaId, body.TipoPagoId, ct)
                            .ConfigureAwait(false));
                    if (tipoPagoError is not null)
                        return tipoPagoError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario est? cerrada.");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotaPagoLibre");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotaPagoLibreAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ImporteRecibido,
                                usuarioId,
                                body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuotaPagoLibre");
                        var detail = "No se pudo registrar el pago con importe libre.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuotaImporteLibre")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuotaPagoLibre. Paridad CajaDiarioBL.PagarCuotaPagoLibre / CreditoController.PagarCuotasImporteLibre.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/credito/pagar-cuota-importe-libre-mixto",
                async Task<Results<Ok<PagoCajaMixtoResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotaPagoLibreMixtoRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    if (body.Medios is null || body.Medios.Count < 2)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "El cobro mixto requiere al menos 2 medios (p. ej. Yape/Plin/BCP + efectivo).");
                    }

                    var total = body.Medios.Sum(m => m.Importe);
                    var moneyError = ProblemResults.IfInvalid(
                        MoneyRules.RequirePositive(total, "suma de medios"));
                    if (moneyError is not null)
                    {
                        return moneyError;
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
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario está cerrada.");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotaPagoLibreMixto");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotaPagoLibreMixtoAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                usuarioId,
                                body.Medios,
                                ct)
                            .ConfigureAwait(false);
                        if (response.MovimientoCajaIds.Count < 1)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No procesado",
                                detail: "El cobro mixto no generó movimientos.");
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        log.LogWarning(ex, "Parámetros de cobro mixto inválidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Revise los medios e importes."
                                : ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cobro mixto no ejecutado");
                        return TypedResults.Problem(
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "No se pudo completar el cobro mixto."
                                : ex.Message,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Operación incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar cobro mixto (usp_PagarCuotaPagoLibre)");
                        var detail = "No se pudo registrar el cobro mixto.";
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
            .WithName("CreditoPagarCuotaImporteLibreMixto")
            .WithSummary(
                "Escritura: N× CREDITO.usp_PagarCuotaPagoLibre en una transacción (Yape/Plin/BCP/Interbank/Nación + efectivo). CreditoRolOperador.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaMixtoResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost(
                "/api/v1/credito/pagar-cuota-con-mora",
                async Task<Results<Ok<PagarCuotaConMoraResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotaConMoraRequest body,
                    ICajaPagoMoraOrchestrator moraOrchestrator,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IOficinaMedioPagoService mediosPago,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var moneyError = ProblemResults.IfInvalid(
                        MoneyRules.RequirePositive(body.ImporteRecibido, "importeRecibido"));
                    if (moneyError is not null)
                        return moneyError;

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var (sesionTipo, sesionTipoErr) = await SesionOficina
                        .ResolverAsync(httpContext, oficinas, null, ct)
                        .ConfigureAwait(false);
                    if (sesionTipoErr is not null)
                        return sesionTipoErr;

                    var tipoPagoMora = body.TipoPagoId < 1 ? 1 : body.TipoPagoId;
                    var medioError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(tipoPagoMora, sesionTipo!.EsCentral)
                        ?? TipoPagoRules.RequireDigitalFecha(
                            tipoPagoMora,
                            body.FechaPagoTransferencia)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(body.OficinaId, tipoPagoMora, ct)
                            .ConfigureAwait(false));
                    if (medioError is not null)
                        return medioError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario est? cerrada.");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotaConMora");
                    try
                    {
                        var response = await moraOrchestrator
                            .ProcesarPagoCuotaConMoraAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                body.ImporteRecibido,
                                usuarioId,
                                body.TipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                body.EsUltimaCuota,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

                        var mensaje = body.EsUltimaCuota
                            ? "Cuota final procesada y moras acumuladas liquidadas con ?xito."
                            : $"Pago registrado (mov. {response.ResultId}).";
                        return TypedResults.Ok(
                            new PagarCuotaConMoraResponse(response.ResultId, mensaje, body.EsUltimaCuota));
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error en pago con mora postergada");
                        var detail = "No se pudo procesar el pago con mora postergada.";
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
            .WithName("CreditoPagarCuotaConMora")
            .WithSummary(
                "Escritura: paridad CajaDiarioBL.ProcesarPagoCompleto / ProcesarPagoCuotaConMora (pago libre + liquidaci?n mora).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagarCuotaConMoraResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/pagar-cuotas-cancelacion",
                async Task<Results<Ok<PagoCajaResultResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    PagarCuotasCancelacionRequest body,
                    ICajaPagoWriteService cajaPago,
                    ICajaDiarioOficinaReadService cajaDiarioOficina,
                    ICreditoOficinaReadService creditoOficina,
                    IEntradaSalidaCajaDiarioReadService entradaSalidaRead,
                    IDatabaseTimeProvider databaseTime,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idError = CajaCreditoWriteGuards.ValidateBodyIds(body.OficinaId, body.CajaDiarioId, body.CreditoId);
                    if (idError is not null)
                    {
                        return idError;
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var (sesionTipo, sesionTipoErr) = await SesionOficina
                        .ResolverAsync(httpContext, oficinas, null, ct)
                        .ConfigureAwait(false);
                    if (sesionTipoErr is not null)
                        return sesionTipoErr;

                    var tipoPagoId = body.TipoPagoId < 1 ? 1 : body.TipoPagoId;
                    var tipoPagoError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(tipoPagoId, sesionTipo!.EsCentral)
                        ?? TipoPagoRules.RequireDigitalFecha(
                            tipoPagoId,
                            body.FechaPagoTransferencia)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(body.OficinaId, tipoPagoId, ct)
                            .ConfigureAwait(false));
                    if (tipoPagoError is not null)
                        return tipoPagoError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var scopeError = await CajaCreditoWriteGuards
                        .ValidateCajaYCreditoOficinaAsync(
                            body.OficinaId,
                            body.CajaDiarioId,
                            body.CreditoId,
                            cajaDiarioOficina,
                            creditoOficina,
                            ct)
                        .ConfigureAwait(false);
                    if (scopeError is not null)
                    {
                        return scopeError;
                    }

                    if (await entradaSalidaRead.CajaDiarioEstaCerradaAsync(body.CajaDiarioId, ct).ConfigureAwait(false))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: "La caja diario est? cerrada.");
                    }

                    var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                    if (serverTime is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos",
                            detail: "No se pudo obtener la fecha del servidor (usp_FechaBD).");
                    }

                    var log = loggerFactory.CreateLogger("PagarCuotasCancelacion");
                    try
                    {
                        var response = await cajaPago
                            .PagarCuotasCancelacionAsync(
                                body.CajaDiarioId,
                                body.CreditoId,
                                usuarioId,
                                serverTime.Value,
                                tipoPagoId,
                                body.FechaPagoTransferencia ?? string.Empty,
                                ct)
                            .ConfigureAwait(false);
                        var pagoError = CajaCreditoWriteGuards.MapPagoResult(response);
                        if (pagoError is not null)
                        {
                            return pagoError;
                        }

                        return TypedResults.Ok(response);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Cadena de conexi?n no configurada");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al ejecutar CREDITO.usp_PagarCuotasCancelacion");
                        var detail = "No se pudo registrar el pago por cancelaci?n.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoPagarCuotasCancelacion")
            .WithSummary(
                "Escritura: CREDITO.usp_PagarCuotasCancelacion. Paridad CajaDiarioBL.PagarCuotasCancelacion / CreditoController.PagarCuotasCancelacion.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolOperador)
            .Produces<PagoCajaResultResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static ProblemHttpResult? TryReadOficinaYAplicar(
        IFormCollection form,
        HttpContext httpContext,
        out int oficinaId,
        out bool aplicarAutos)
    {
        oficinaId = 0;
        aplicarAutos =
            string.Equals(form["aplicarAutos"].ToString(), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(form["aplicarAutos"].ToString(), "1", StringComparison.OrdinalIgnoreCase);

        var oficinaRaw = form["oficinaId"].ToString();
        if (!int.TryParse(oficinaRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out oficinaId) ||
            oficinaId < 1)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Parámetros inválidos",
                detail: "oficinaId es obligatorio y debe ser un entero >= 1.");
        }

        return CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
    }
}
