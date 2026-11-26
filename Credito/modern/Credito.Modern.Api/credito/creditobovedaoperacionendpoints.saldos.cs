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

/// <summary>Bóveda — saldos, validaciones y caja chica.</summary>
internal static partial class CreditoBovedaOperacionEndpoints
{
    private static void MapBovedaSaldosYCajaChica(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/validar-cierre-saldos",
                async Task<Results<Ok<ValidarCierreSaldosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreSaldos");
                    try
                    {
                        var response = await saldosCierre
                            .ValidarCierreMasivoAsync(oficinaId, ct)
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
                        log.LogWarning(ex, "Par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre masivo de saldos");
                        var detail = "No se pudo validar el cierre masivo.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreSaldos")
            .WithSummary(
                "Solo lectura: paridad SaldosController.ValidarCierre (previo a cerrar-cajas-diarios / Transferir).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreSaldosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/validar-cierre-boveda",
                async Task<Results<Ok<ValidarCierreSaldosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreBoveda");
                    try
                    {
                        var response = await saldosCierre
                            .ValidarCierreBovedaAsync(oficinaId, ct)
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
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Los parámetros enviados no son válidos."
                                : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre de bóveda");
                        var detail = "No se pudo validar el cierre de bóveda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreBoveda")
            .WithSummary(
                "Solo lectura: paridad BovedaController.ValidarCierre (previo a cerrar-boveda principal).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreSaldosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/validar-cierre-boveda-temporal",
                async Task<Results<Ok<ValidarCierreSaldosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreBovedaTemporal");
                    try
                    {
                        var response = await saldosCierre
                            .ValidarCierreBovedaTemporalAsync(oficinaId, ct)
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
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Los parámetros enviados no son válidos."
                                : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre de bóveda temporal");
                        var detail = "No se pudo validar el cierre de bóveda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreBovedaTemporal")
            .WithSummary(
                "Solo lectura: cajas cerradas/enviadas + cuadre de la temporal (no exige cuadre de la principal).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreSaldosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/saldos-caja-diario",
                async Task<Results<Ok<SaldoCajaSesionPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCajaDiarioReadService saldos,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct,
                    string? buscar = null,
                    int page = 1,
                    int pageSize = 25) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("SaldosCajaDiario");
                    try
                    {
                        var pagina = await saldos
                            .ListarCajaDiarioPorOficinaAsync(oficinaId, buscar, page, pageSize, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(pagina);
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
                        log.LogError(ex, "Error al listar saldos caja diario");
                        var detail = "No se pudo listar saldos de caja diario.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSaldosCajaDiario")
            .WithSummary(
                "Paridad SaldosController.ListarSaldoCajaDiario (paginado en servidor como el jqGrid; totales sobre el filtro completo).")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<SaldoCajaSesionPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/saldos-caja-chica-diario",
                async Task<Results<Ok<SaldoCajaSesionPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCajaDiarioReadService saldos,
                    IOficinaReadService oficinas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct,
                    string? buscar = null,
                    int page = 1,
                    int pageSize = 25) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    var (_, sedeErr) = await GestorInformeReportAccess.EnsureOficinaConsultaAsync(
                        httpContext, oficinas, oficinaId, ct).ConfigureAwait(false);
                    if (sedeErr is not null)
                    {
                        return sedeErr;
                    }

                    var log = loggerFactory.CreateLogger("SaldosCajaChicaDiario");
                    try
                    {
                        var pagina = await saldos
                            .ListarCajaChicaDiarioAsync(oficinaId, buscar, page, pageSize, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(pagina);
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
                        log.LogError(ex, "Error al listar saldos caja chica diario");
                        var detail = "No se pudo listar saldos de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSaldosCajaChicaDiario")
            .WithSummary(
                "Paridad SaldosController.ListarSaldoCajaChicaDiario (paginado en servidor; filtrado por oficina).")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<SaldoCajaSesionPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/saldos-caja-diario-boveda",
                async Task<Results<Ok<SaldoCajaSesionPageDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int bovedaId,
                    int oficinaId,
                    ISaldosCajaDiarioReadService saldos,
                    IBovedaOficinaReadService bovedaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct,
                    string? buscar = null,
                    int page = 1,
                    int pageSize = 25) =>
                {
                    if (bovedaId < 1 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "bovedaId y oficinaId deben ser >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("SaldosCajaDiarioBoveda");
                    try
                    {
                        var oficinaBoveda = await bovedaOficina
                            .GetOficinaIdByBovedaIdAsync(bovedaId, ct)
                            .ConfigureAwait(false);
                        if (oficinaBoveda is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe la bóveda indicada.");
                        }

                        if (oficinaBoveda.Value != oficinaId)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status403Forbidden,
                                title: "Prohibido",
                                detail: "La bóveda no pertenece a la oficina de la sesión.");
                        }

                        var pagina = await saldos
                            .ListarCajaDiarioBovedaAsync(bovedaId, buscar, page, pageSize, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(pagina);
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
                        log.LogError(ex, "Error al listar saldos caja diario b?veda");
                        var detail = "No se pudo listar saldos por b?veda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoSaldosCajaDiarioBoveda")
            .WithSummary("Paridad SaldosController.ListarSaldoCajaDiarioBoveda (paginado en servidor).")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<SaldoCajaSesionPageDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/validar-cierre-caja-chica",
                async Task<Results<Ok<ValidarCierreSaldosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    ISaldosCierreReadService saldosCierre,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ValidarCierreCajaChica");
                    try
                    {
                        var response = await saldosCierre
                            .ValidarCierreCajaChicaAsync(oficinaId, ct)
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
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al validar cierre caja chica");
                        var detail = "No se pudo validar el cierre de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoValidarCierreCajaChica")
            .WithSummary(
                "Solo lectura: valida cierre de caja chica filtrado por oficina.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ValidarCierreSaldosResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/actualizar-datos-post-cierre-boveda",
                async Task<Results<Ok, ProblemHttpResult>> (
                    HttpContext httpContext,
                    ActualizarDatosPostCierreBovedaRequest body,
                    ISaldosCierreWriteService saldosCierreWrite,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioCierreId)
                        || usuarioCierreId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:usuario_id.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("ActualizarDatosPostCierreBoveda");
                    try
                    {
                        await saldosCierreWrite
                            .ActualizarDatosPostCierreBovedaAsync(body.OficinaId, usuarioCierreId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok();
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
                        log.LogError(ex, "Error al actualizar datos post cierre b?veda");
                        var detail =
                            "No se pudieron actualizar saldos de cartera, calificaci?n ni intento de cierre gerencial.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoActualizarDatosPostCierreBoveda")
            .WithSummary(
                "Escritura: usp_ActualizarSaldoCartera + usp_CalificarCliente + usp_IntentarGenerarCierreGerencialMensual. Paridad CajaDiarioBL.ActualizarDatosPostCierreBoveda.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/cajas-abiertas-transferencia-boveda",
                async Task<Results<Ok<List<CajaAbiertaTransferenciaRowDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaMovReadService bovedaMov,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
                    }

                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                    {
                        return oficinaError;
                    }

                    var log = loggerFactory.CreateLogger("CajasAbiertasTransferenciaBoveda");
                    try
                    {
                        var items = await bovedaMov
                            .ListarCajasAbiertasParaTransferenciaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(items);
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
                        log.LogError(ex, "Error al listar cajas abiertas");
                        var detail = "No se pudo listar cajas abiertas.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoCajasAbiertasTransferenciaBoveda")
            .WithSummary("Solo lectura: paridad CajaBL.ListarCajasAbiertas (combo transferir b?veda ??? caja).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CajaAbiertaTransferenciaRowDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/ingreso-egreso-boveda",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    IngresoEgresoBovedaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            TesoreriaValidacion.ValidarMovimientoOficina(body.OficinaId, body.Importe, body.Descripcion),
                            IdRules.RequirePositive(body.TipoOperacionId, "tipoOperacionId")));
                    if (edgeError is not null)
                        return edgeError;

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

                    var log = loggerFactory.CreateLogger("IngresoEgresoBoveda");
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

                        var result = await bovedaMovWrite
                            .IngresoEgresoAsync(
                                body.OficinaId,
                                body.Importe,
                                body.Descripcion,
                                body.TipoOperacionId,
                                body.TipoPagoId,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (result is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo registrar",
                                detail: "Tipo de operaci?n inv?lido, b?veda no disponible o error de negocio.");
                        }

                        return TypedResults.Ok(result);
                    }
                    catch (InvalidOperationException ex) when (
                        ex.Message.Contains("Saldo insuficiente", StringComparison.OrdinalIgnoreCase))
                    {
                        log.LogWarning(ex, "Egreso bóveda sin saldo en medio");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status422UnprocessableEntity,
                            title: "Saldo insuficiente",
                            detail: ex.Message);
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
                        log.LogError(ex, "Error en ingreso/egreso b?veda");
                        var detail = "No se pudo registrar ingreso/egreso de b?veda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoIngresoEgresoBoveda")
            .WithSummary(
                "Escritura: paridad BovedaMovBL.IngresoEgresoBovedaCaja + usp_ActualizarSaldosBoveda. Tipo operaci?n debe tener IndBoveda.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda-caja",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaCajaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            TesoreriaValidacion.ValidarMovimientoOficina(body.OficinaId, body.Importe, body.Descripcion),
                            IdRules.RequirePositive(body.CajaId, "cajaId")));
                    if (edgeError is not null)
                        return edgeError;

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

                    var tipoOrigen = body.TipoPagoOrigenId < 1 ? (short)1 : body.TipoPagoOrigenId;
                    var tipoDestino = body.TipoPagoDestinoId < 1 ? (short)1 : body.TipoPagoDestinoId;
                    var tipoPagoError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireParCobroPermitido(
                            tipoOrigen,
                            tipoDestino,
                            sesionTipo!.EsCentral)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(
                                body.OficinaId,
                                tipoOrigen,
                                ct,
                                "tipoPagoOrigenId")
                            .ConfigureAwait(false)
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(
                                body.OficinaId,
                                tipoDestino,
                                ct,
                                "tipoPagoDestinoId")
                            .ConfigureAwait(false));
                    if (tipoPagoError is not null)
                        return tipoPagoError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("TransferirBovedaCaja");
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

                        var (error, result) = await bovedaMovWrite
                            .TransferirACajaAsync(
                                body.OficinaId,
                                body.CajaId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                serverTime.Value,
                                tipoOrigen,
                                tipoDestino,
                                ct)
                            .ConfigureAwait(false);

                        if (error is not null || result is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error ?? "Bóveda o caja diario no disponible en la oficina.");
                        }

                        return TypedResults.Ok(result);
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
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al transferir bóveda a caja");
                        var detail = string.IsNullOrWhiteSpace(ex.Message)
                            ? "No se pudo transferir de bóveda a caja."
                            : $"No se pudo transferir de bóveda a caja. Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirBovedaCaja")
            .WithSummary(
                "Escritura: paridad BovedaMovBL.TransferirBovedaCaja (BovedaMov TRS + MovimientoCaja TRE + usp_ActualizarSaldosBoveda). Soporta embudo tipoPagoOrigenId → tipoPagoDestinoId (default efectivo).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda-analista",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaAnalistaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            TesoreriaValidacion.ValidarMovimientoOficina(body.OficinaId, body.Importe, body.Descripcion),
                            IdRules.RequirePositive(body.UsuarioAnalistaId, "usuarioAnalistaId"),
                            IdRules.RequirePositive(body.TipoPagoOrigenId, "tipoPagoOrigenId")));
                    if (edgeError is not null)
                        return edgeError;

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
                        TipoPagoRules.RequireCobroPermitido(
                            body.TipoPagoOrigenId,
                            sesionTipo!.EsCentral,
                            "tipoPagoOrigenId")
                        ?? await mediosPago
                            .ValidateMedioActivoAsync(
                                body.OficinaId,
                                body.TipoPagoOrigenId,
                                ct,
                                "tipoPagoOrigenId")
                            .ConfigureAwait(false));
                    if (tipoPagoError is not null)
                        return tipoPagoError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("TransferirBovedaAnalista");
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

                        var (error, result) = await bovedaMovWrite
                            .TransferirAAnalistaAsync(
                                body.OficinaId,
                                body.UsuarioAnalistaId,
                                body.TipoPagoOrigenId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (error is not null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al transferir b?veda a analista");
                        var detail = "No se pudo transferir de b?veda al analista.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirBovedaAnalista")
            .WithSummary(
                "Escritura: paridad BovedaController.TransferirAAnalista. Valida saldo del medio en ledger y embudo a efectivo (TipoPagoDestino=1).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-boveda-caja-chica",
                async Task<Results<Ok<BovedaMovOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirBovedaCajaChicaRequest body,
                    IBovedaMovWriteService bovedaMovWrite,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        TesoreriaValidacion.ValidarMovimientoOficina(body.OficinaId, body.Importe, body.Descripcion));
                    if (edgeError is not null)
                        return edgeError;

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

                    var log = loggerFactory.CreateLogger("TransferirBovedaCajaChica");
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

                        var (error, result) = await bovedaMovWrite
                            .TransferirACajaChicaAsync(
                                body.OficinaId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                serverTime.Value,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al transferir b?veda a caja chica");
                        var detail = "No se pudo transferir de b?veda a caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirBovedaCajaChica")
            .WithSummary(
                "Escritura: bóveda→caja chica (efectivo). Resuelve caja chica abierta de la oficina JWT y valida saldo del medio.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMovOperacionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/transferir-cierre-caja-chica",
                async Task<Results<Ok<TransferirCierreCajaChicaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    TransferirCierreCajaChicaRequest body,
                    ICajaChicaDiarioWriteService cajaChicaDiarioWrite,
                    ISaldosCierreReadService saldosCierre,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "oficinaId debe ser un entero >= 1.");
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

                    var validacion = await saldosCierre.ValidarCierreCajaChicaAsync(body.OficinaId, ct).ConfigureAwait(false);
                    if (!validacion.PuedeCerrar)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: "Conflicto",
                            detail: validacion.Mensaje);
                    }

                    var log = loggerFactory.CreateLogger("TransferirCierreCajaChica");
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

                        var (error, result) = await cajaChicaDiarioWrite
                            .TransferirCierreABovedaAsync(body.OficinaId, usuarioId, serverTime.Value, ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo transferir",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al transferir cierre caja chica a b?veda");
                        var detail = "No se pudo transferir el cierre de caja chica a b?veda.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoTransferirCierreCajaChica")
            .WithSummary(
                "Escritura: paridad CajaChicaDiarioBL.TransferirCajaChicaDiarioBoveda. Requiere validar-cierre-caja-chica OK.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<TransferirCierreCajaChicaResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/asignar-boveda-temporal",
                async Task<Results<Ok<AsignarBovedaTemporalResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    AsignarBovedaTemporalRequest body,
                    IBovedaTemporalWriteService bovedaTemporalWrite,
                    IOficinaReadService oficinas,
                    IOficinaMedioPagoService mediosPago,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            TesoreriaValidacion.ValidarMovimientoOficina(body.OficinaId, body.Importe, body.Descripcion),
                            body.UsuarioId < 0 ? "usuarioId debe ser >= 0." : null));
                    if (edgeError is not null)
                        return edgeError;

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

                    var tipoPagoId = body.TipoPagoId < 1 ? (short)1 : body.TipoPagoId;
                    var tipoPagoError = ProblemResults.IfInvalid(
                        TipoPagoRules.RequireCobroPermitido(tipoPagoId, sesionTipo!.EsCentral)
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

                    var log = loggerFactory.CreateLogger("AsignarBovedaTemporal");
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

                        var (error, result) = await bovedaTemporalWrite
                            .AsignarOTransferirAsync(
                                body.OficinaId,
                                body.Importe,
                                body.Descripcion,
                                body.UsuarioId,
                                usuarioId,
                                serverTime.Value,
                                tipoPagoId,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo asignar b?veda temporal",
                                detail: error);
                        }

                        return TypedResults.Ok(result!);
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
                        log.LogError(ex, "Error al asignar b?veda temporal");
                        var detail = "No se pudo asignar o transferir a b?veda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoAsignarBovedaTemporal")
            .WithSummary(
                "Escritura: paridad BovedaController.AsignarBovedaTemporal. usuarioId>0 crea temporal+ENCARGADO; usuarioId=0 solo transferir.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolEncargadoOAdministrador)
            .Produces<AsignarBovedaTemporalResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapPost(
                "/api/v1/credito/entrada-salida-caja-chica-diario",
                async Task<Results<Ok<EntradaSalidaCajaDiarioResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    EntradaSalidaCajaChicaDiarioRequest body,
                    ICajaChicaEntradaSalidaWriteService cajaChicaEntradaSalida,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var edgeError = ProblemResults.IfInvalid(
                        ValidationGate.First(
                            IdRules.RequirePositive(body.OficinaId, "oficinaId"),
                            IdRules.RequirePositive(body.PersonaId, "personaId"),
                            IdRules.RequirePositive(body.TipoOperacionId, "tipoOperacionId"),
                            MoneyRules.RequirePositive(body.Importe, "importe"),
                            StringRules.RequireText(body.Descripcion, "descripcion", StringRules.MaxGlosa)));
                    if (edgeError is not null)
                        return edgeError;

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

                    var log = loggerFactory.CreateLogger("EntradaSalidaCajaChicaDiario");
                    try
                    {
                        var (error, result) = await cajaChicaEntradaSalida
                            .EjecutarAsync(
                                body.OficinaId,
                                body.PersonaId,
                                body.TipoOperacionId,
                                body.Importe,
                                body.Descripcion,
                                usuarioId,
                                ct)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(error))
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo registrar movimiento",
                                detail: error);
                        }

                        if (result!.ResultCode < 0)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status422UnprocessableEntity,
                                title: "No se pudo registrar movimiento",
                                detail: $"El procedimiento devolvi? c?digo {result.ResultCode}.");
                        }

                        return TypedResults.Ok(result);
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
                        log.LogError(ex, "Error en entrada/salida caja chica");
                        var detail = "No se pudo registrar entrada/salida de caja chica.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoEntradaSalidaCajaChicaDiario")
            .WithSummary(
                "Escritura: CREDITO.usp_EntradaSalidaCajaChicaDiario. Caja chica abierta del usuario JWT. Par?metro SQL Decripcion.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<EntradaSalidaCajaDiarioResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}