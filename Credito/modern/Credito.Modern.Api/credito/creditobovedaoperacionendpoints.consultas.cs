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

/// <summary>Bóveda — consultas / lecturas.</summary>
internal static partial class CreditoBovedaOperacionEndpoints
{
    private static void MapBovedaConsultas(WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/boveda-abierta",
                async Task<Results<Ok<BovedaAbiertaDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaAbiertaReadService bovedaAbierta,
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

                    var log = loggerFactory.CreateLogger("BovedaAbierta");
                    try
                    {
                        var boveda = await bovedaAbierta
                            .ObtenerAbiertaPorOficinaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        if (boveda is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No existe b?veda abierta para la oficina.");
                        }

                        return TypedResults.Ok(boveda);
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
                        log.LogError(ex, "Error al consultar b?veda abierta");
                        var detail = "No se pudo consultar la b?veda abierta.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoBovedaAbierta")
            .WithSummary(
                "Solo lectura: b?veda abierta de la oficina (IndCierre=0), ORDER BY IndTemporal, BovedaId ??? paridad operaciones de escritura y BovedaController.Index.")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaAbiertaDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/bovedas-destino-transferencia",
                async Task<Results<Ok<List<BovedaDestinoTransferenciaDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaOficinaReadService bovedaOficina,
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

                    var log = loggerFactory.CreateLogger("BovedasDestinoTransferencia");
                    try
                    {
                        var rows = await bovedaOficina
                            .ListarDestinosTransferenciaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(rows.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "B?vedas destino transferencia: configuraci?n incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "B?vedas destino transferencia: par?metros inv?lidos");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar b?vedas destino para transferencia");
                        var detail = "No se pudo listar b?vedas destino para transferencia.";
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
            .WithName("CreditoBovedasDestinoTransferencia")
            .WithSummary(
                "Destinos de red: Central lista sucursales abiertas; sucursal lista solo la bóveda Central.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<BovedaDestinoTransferenciaDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/boveda-red-posicion",
                async Task<Results<Ok<List<BovedaRedPosicionDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaOficinaReadService bovedaOficina,
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

                    var log = loggerFactory.CreateLogger("BovedaRedPosicion");
                    try
                    {
                        var rows = await bovedaOficina
                            .ListarPosicionRedAsync(ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(rows.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Posici?n red b?veda: configuraci?n incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar posici?n de efectivo de la red");
                        var detail = "No se pudo listar la posici?n de efectivo de la red.";
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
            .WithName("CreditoBovedaRedPosicion")
            .WithSummary("Efectivo de oficina por sede (b?veda + cajas + chica). No mezcla cartera. CreditoUser.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<BovedaRedPosicionDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/boveda-medios-red",
                async Task<Results<Ok<BovedaMediosRedDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaOficinaReadService bovedaOficina,
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

                    var log = loggerFactory.CreateLogger("BovedaMediosRed");
                    try
                    {
                        var dto = await bovedaOficina
                            .ListarMediosRedAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Medios red bóveda: configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al listar medios de la red");
                        var detail = "No se pudo listar los medios de pago de la red.";
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
            .WithName("CreditoBovedaMediosRed")
            .WithSummary(
                "Medios de red: Central muestra Gerente/René + Yape/Interbank/BCP/Plin por sede; sucursal solo su oficina.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaMediosRedDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/boveda-saldo-medio",
                async Task<Results<Ok<BovedaSaldoMedioDto>, NotFound, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int bovedaId,
                    short tipoPagoId,
                    IBovedaOficinaReadService bovedaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId < 1 || tipoPagoId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "bovedaId y tipoPagoId deben ser enteros >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("BovedaSaldoMedio");
                    try
                    {
                        var oficinaBoveda = await bovedaOficina
                            .GetOficinaIdByBovedaIdAsync(bovedaId, ct)
                            .ConfigureAwait(false);
                        if (oficinaBoveda is null or < 1)
                        {
                            return TypedResults.NotFound();
                        }

                        var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(
                            httpContext,
                            oficinaBoveda.Value);
                        if (oficinaError is not null)
                        {
                            return oficinaError;
                        }

                        var dto = await bovedaOficina
                            .ObtenerSaldoMedioDisponibleAsync(bovedaId, tipoPagoId, ct)
                            .ConfigureAwait(false);
                        if (dto is null)
                        {
                            return TypedResults.NotFound();
                        }

                        return TypedResults.Ok(dto);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        log.LogWarning(ex, "Parámetros inválidos saldo medio");
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message)
                                ? "Los parámetros enviados no son válidos."
                                : ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Saldo medio bóveda: configuración incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuración incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Error al leer saldo medio de bóveda");
                        var detail = "No se pudo consultar el saldo del medio.";
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
            .WithName("CreditoBovedaSaldoMedio")
            .WithSummary(
                "Saldo disponible de un medio en bóveda (SI cuenta + neto mov; paridad usp_TransferirBoveda).")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaSaldoMedioDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet(
                "/api/v1/credito/boveda-transferencias-pendientes",
                async Task<Results<Ok<List<BovedaTransferenciaPendienteDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int bovedaId,
                    IBovedaOficinaReadService bovedaOficina,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (bovedaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inv?lida",
                            detail: "bovedaId debe ser un entero >= 1.");
                    }

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
                                detail: "No existe la b?veda indicada.");
                        }

                        var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(
                            httpContext,
                            oficinaBoveda.Value);
                        if (oficinaError is not null)
                        {
                            return oficinaError;
                        }

                        var rows = await bovedaOficina
                            .ListarTransferenciasPendientesAsync(bovedaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(rows.ToList());
                    }
                    catch (InvalidOperationException ex)
                    {
                        var log = loggerFactory.CreateLogger("BovedaTransferenciasPendientes");
                        log.LogWarning(ex, "Transferencias pendientes: configuraci?n incompleta");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Par?metros inv?lidos",
                            detail: string.IsNullOrWhiteSpace(ex.Message) ? "Los parámetros enviados no son válidos." : ex.Message);
                    }
                    catch (DbException ex)
                    {
                        var log = loggerFactory.CreateLogger("BovedaTransferenciasPendientes");
                        log.LogError(ex, "Error al listar transferencias pendientes de b?veda");
                        var detail = "No se pudo listar transferencias pendientes.";
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
            .WithName("CreditoBovedaTransferenciasPendientes")
            .WithSummary(
                "Transferencias interoficina pendientes hacia la b?veda (paridad ListarTransferencias).")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<BovedaTransferenciaPendienteDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



        app.MapGet(
                "/api/v1/credito/boveda-estado-dinero",
                async Task<Results<Ok<BovedaEstadoDineroDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaAbiertaReadService bovedaAbierta,
                    IBovedaEstadoDineroReadService estadoDinero,
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

                    var log = loggerFactory.CreateLogger("BovedaEstadoDinero");
                    try
                    {
                        var boveda = await bovedaAbierta.ObtenerAbiertaPorOficinaAsync(oficinaId, ct).ConfigureAwait(false);
                        var saldo = boveda?.SaldoFinal ?? 0m;
                        var dto = await estadoDinero
                            .ObtenerAsync(oficinaId, saldo, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Boveda estado dinero");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Boveda estado dinero");
                        var detail = "No se pudo obtener el estado de dinero.";
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
            .WithName("CreditoBovedaEstadoDinero")
            .WithSummary("Paridad Boveda/Index ??? KPIs estado de dinero y total fondo.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaEstadoDineroDto>(StatusCodes.Status200OK, "application/json");



        app.MapGet(
                "/api/v1/credito/boveda-listar",
                async Task<Results<Ok<BovedaListadoResultDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int? page,
                    int? pageSize,
                    IBovedaListadoReadService listado,
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

                    var log = loggerFactory.CreateLogger("BovedaListar");
                    try
                    {
                        var (rows, total) = await listado
                            .ListarAsync(oficinaId, page ?? 1, pageSize ?? 5, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new BovedaListadoResultDto(rows.ToList(), total, page ?? 1, pageSize ?? 5));
                    }
                    catch (InvalidOperationException ex)
                    {
                        log.LogWarning(ex, "Boveda listar");
                        return TypedResults.Problem(
                            detail: "No se pudo completar la operaci?n por configuraci?n incompleta del servidor.",
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Configuraci?n incompleta");
                    }
                    catch (DbException ex)
                    {
                        log.LogError(ex, "Boveda listar");
                        var detail = "No se pudo listar b?vedas.";
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
            .WithName("CreditoBovedaListar")
            .WithSummary("Paridad ListarBovedaJgrid ??? historial de cierres de b?veda.")
            .WithTags("credito", "boveda")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<BovedaListadoResultDto>(StatusCodes.Status200OK, "application/json");



        app.MapGet(
                "/api/v1/credito/existe-boveda-temporal",
                async Task<Results<Ok<ExisteBovedaTemporalResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    IBovedaOficinaReadService bovedaOficina,
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

                    var log = loggerFactory.CreateLogger("ExisteBovedaTemporal");
                    try
                    {
                        var existe = await bovedaOficina
                            .ExisteBovedaTemporalAbiertaAsync(oficinaId, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(new ExisteBovedaTemporalResponse(existe));
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
                        log.LogError(ex, "Error al consultar b?veda temporal");
                        var detail = "No se pudo consultar la b?veda temporal.";
                        if (env.IsDevelopment())
                            detail += $" Detalle: {ex.Message}";
                        return TypedResults.Problem(
                            detail: detail,
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos");
                    }
                })
            .WithName("CreditoExisteBovedaTemporal")
            .WithSummary("Solo lectura: paridad BovedaController.ExisteBovedaTemporal (b?veda abierta IndTemporal=1).")
            .WithTags("credito")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ExisteBovedaTemporalResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);



    }
}