using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.Time;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Caja;

internal sealed record CajaPorCajeroResponse(string Denominacion);
internal static class CajaMaestroEndpoints
{
    public static void MapCajaMaestroEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/caja-por-cajero",
                async Task<Results<Ok<CajaPorCajeroResponse>, ProblemHttpResult>> (
                    int usuarioId,
                    ICajaMaestroReadService cajas,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1)
                    {
                        return TypedResults.Ok(new CajaPorCajeroResponse(string.Empty));
                    }

                    var denominacion = await cajas
                        .GetDenominacionPorCajeroAsync(usuarioId, ct)
                        .ConfigureAwait(false);
                    return TypedResults.Ok(new CajaPorCajeroResponse(denominacion ?? string.Empty));
                })
            .WithName("CreditoCajaPorCajero")
            .WithSummary("Denominación de caja asignada al gestor (paridad CajaBL en cobro diario MVC).")
            .WithTags("credito", "caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaPorCajeroResponse>(StatusCodes.Status200OK);

        app.MapGet(
                "/api/v1/cajas/gestion",
                async Task<Results<Ok<CajaGestionPageDto>, ProblemHttpResult>> (
                    string? buscar,
                    int? page,
                    int? pageSize,
                    bool? incluirInactivos,
                    ICajaMaestroReadService cajas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("CajasGestion");
                    try
                    {
                        var result = await cajas
                            .ListGestionAsync(
                                buscar,
                                page ?? 1,
                                pageSize ?? 25,
                                incluirInactivos == true,
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CajasGestion")
            .WithSummary("Paridad CajaController.ListarCajaGrid / CajaBL.LstCajaJGrid.")
            .WithTags("caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<CajaGestionPageDto>();

        app.MapGet(
                "/api/v1/cajas/gestores-activos",
                async Task<Results<Ok<List<CajaGestorItemDto>>, ProblemHttpResult>> (
                    ICajaMaestroReadService cajas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("CajasGestores");
                    try
                    {
                        var items = await cajas.ListGestoresActivosAsync(ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CajasGestoresActivos")
            .WithSummary("Combo Gestor en mantenimiento caja (Usuario activo + Persona).")
            .WithTags("caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CajaGestorItemDto>>();

        app.MapGet(
                "/api/v1/cajas/combo-activas",
                async Task<Results<Ok<List<CajaComboItemDto>>, ProblemHttpResult>> (
                    ICajaMaestroReadService cajas,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("CajasComboActivas");
                    try
                    {
                        var items = await cajas.ListCajasActivasAsync(ct).ConfigureAwait(false);
                        return TypedResults.Ok(items);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("CajasComboActivas")
            .WithSummary("Paridad CajaController.CargarComboOficina (cajas activas id/denominación).")
            .WithTags("caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<List<CajaComboItemDto>>();

        app.MapPost(
                "/api/v1/cajas/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarCajaRequest body,
                    ICajaMaestroWriteService write,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (body.OficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId debe ser >= 1.");
                    }

                    if (string.IsNullOrWhiteSpace(body.Denominacion))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "denominacion es obligatoria.");
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                    if (serverTime is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos",
                            detail: "No se pudo obtener la fecha del servidor.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "GuardarCaja",
                        ct,
                        token => write.GuardarAsync(body, usuarioId, serverTime.Value, token)).ConfigureAwait(false);
                })
            .WithName("GuardarCaja")
            .WithSummary("Paridad CajaController.GuardarCaja (alta: IndAbierto=0, UsuarioReg/FechaReg).")
            .WithTags("caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/cajas/{cajaId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int cajaId,
                    ICajaMaestroWriteService write,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (cajaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "cajaId debe ser >= 1.");
                    }

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false);
                    if (serverTime is null)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable,
                            title: "Error de base de datos",
                            detail: "No se pudo obtener la fecha del servidor.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "ActivarCaja",
                        ct,
                        token => write.ActivarAsync(cajaId, usuarioId, serverTime.Value, token)).ConfigureAwait(false);
                })
            .WithName("ActivarCaja")
            .WithSummary("Paridad CajaController.Activar (toggle Estado).")
            .WithTags("caja")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();
    }

    private static ProblemHttpResult ReadError(ILogger log, IHostEnvironment env, Exception ex)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Cadena de conexión no configurada");
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Error en caja maestro");
        var detail = "No se pudo completar la operación.";
        if (env.IsDevelopment())
            detail += $" Detalle: {ex.Message}";
        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }

    private static async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> WriteAsync(
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        string operation,
        CancellationToken ct,
        Func<CancellationToken, Task<MaestroOperacionResponse>> action)
    {
        var log = loggerFactory.CreateLogger(operation);
        try
        {
            var response = await action(ct).ConfigureAwait(false);
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
            log.LogError(ex, "Error en {Operation}", operation);
            var detail = "No se pudo completar la operación.";
            if (env.IsDevelopment())
                detail += $" Detalle: {ex.Message}";
            return TypedResults.Problem(
                detail: detail,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Error de base de datos");
        }
    }
}
