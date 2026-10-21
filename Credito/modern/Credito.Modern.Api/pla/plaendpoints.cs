using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Pla;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;

namespace Credito.Modern.Api.Pla;

internal static class PlaEndpoints
{
    public static void MapPlaEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/pla/parametro",
                async Task<Results<Ok<PlaParametroDto>, ProblemHttpResult>> (
                    IPlaService pla,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    try
                    {
                        var row = await pla.ObtenerParametroAsync(ct).ConfigureAwait(false);
                        if (row is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status503ServiceUnavailable,
                                title: "Configuración incompleta",
                                detail: "Falta CREDITO.PlaParametro. Aplique deploy/sql/2026-10-21-pla-uif.sql.");
                        }

                        return TypedResults.Ok(row);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return Fallo(loggerFactory, env, ex, "No se pudo leer el parámetro PLA.");
                    }
                })
            .WithName("PlaParametro")
            .WithTags("pla")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost(
                "/api/v1/pla/parametro",
                async Task<Results<Ok<PlaParametroDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GuardarPlaParametroRequest body,
                    IPlaService pla,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    try
                    {
                        var row = await pla.GuardarParametroAsync(body, usuarioId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(row);
                    }
                    catch (ArgumentException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (SqlException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return Fallo(loggerFactory, env, ex, "No se pudo guardar el parámetro PLA.");
                    }
                })
            .WithName("PlaGuardarParametro")
            .WithTags("pla")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet(
                "/api/v1/pla/candidatos-ro",
                async Task<Results<Ok<IReadOnlyList<PlaCandidatoRoDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int anio,
                    int mes,
                    IPlaService pla,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    try
                    {
                        var rows = await pla.ListarCandidatosRoAsync(oficinaId, anio, mes, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(rows);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, año y mes son obligatorios.");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return Fallo(loggerFactory, env, ex, "No se pudo listar candidatos RO.");
                    }
                })
            .WithName("PlaCandidatosRo")
            .WithTags("pla")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet(
                "/api/v1/pla/inusual",
                async Task<Results<Ok<IReadOnlyList<PlaInusualDto>>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    int oficinaId,
                    int anio,
                    int mes,
                    IPlaService pla,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, oficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    try
                    {
                        var rows = await pla.ListarInusualAsync(oficinaId, anio, mes, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(rows);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "oficinaId, año y mes son obligatorios.");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return Fallo(loggerFactory, env, ex, "No se pudo listar la bitácora PLA.");
                    }
                })
            .WithName("PlaListarInusual")
            .WithTags("pla")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost(
                "/api/v1/pla/inusual",
                async Task<Results<Ok<RegistrarPlaInusualResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    RegistrarPlaInusualRequest body,
                    IPlaService pla,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var oficinaError = CajaCreditoWriteGuards.ValidateJwtOficina(httpContext, body.OficinaId);
                    if (oficinaError is not null)
                        return oficinaError;

                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                        return usuarioError;

                    try
                    {
                        var row = await pla.RegistrarInusualAsync(body, usuarioId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(row);
                    }
                    catch (ArgumentException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (SqlException ex)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: ex.Message);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return Fallo(loggerFactory, env, ex, "No se pudo registrar la bitácora PLA.");
                    }
                })
            .WithName("PlaRegistrarInusual")
            .WithTags("pla")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);
    }

    private static ProblemHttpResult Fallo(
        ILoggerFactory loggerFactory,
        IHostEnvironment env,
        Exception ex,
        string titulo)
    {
        var log = loggerFactory.CreateLogger("Pla");
        log.LogError(ex, "{Titulo}", titulo);
        var detail = titulo;
        if (env.IsDevelopment())
            detail += $" Detalle: {ex.Message}";
        return TypedResults.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "No disponible",
            detail: detail);
    }
}
