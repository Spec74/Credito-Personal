using System.Globalization;
using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Validation;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Validation;
using Credito.Modern.Application.ValorTablas;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

internal static class CreditoConfigEndpoints
{
    public static void MapCreditoConfigEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/credito/parametros-simulador",
                async Task<Results<Ok<ParametrosSimuladorDto>, ProblemHttpResult>> (
                    IParametrosSimuladorService parametros,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ParametrosSimuladorGet");
                    try
                    {
                        var dto = await parametros.ObtenerAsync(ct).ConfigureAwait(false);
                        return TypedResults.Ok(dto);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("ParametrosSimuladorGet")
            .WithTags("credito-config")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ParametrosSimuladorDto>();

        app.MapPost(
                "/api/v1/credito/parametros-simulador",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    ActualizarParametrosSimuladorRequest body,
                    IParametrosSimuladorService parametros,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!TryNormalizeFactor(body.FactorVariable, out var factorVariable, out var factorVariableError))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: factorVariableError);
                    }

                    if (!TryNormalizeFactor(body.FactorFijo, out var factorFijo, out var factorFijoError))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: factorFijoError);
                    }

                    var log = loggerFactory.CreateLogger("ParametrosSimuladorPost");
                    try
                    {
                        var ok = await parametros
                            .ActualizarAsync(
                                new ActualizarParametrosSimuladorRequest(factorVariable, factorFijo),
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(ok);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("ParametrosSimuladorPost")
            .WithTags("credito-config")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<bool>();

        app.MapPost(
                "/api/v1/credito/generar-ruta-cobros",
                async Task<Results<Ok<GenerarRutaCobrosResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    GenerarRutaCobrosRequest body,
                    IRutaCobrosService rutaCobros,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var idsError = ProblemResults.IfInvalid(
                        IdRules.RequireAllPositive(body.CreditoIds, "creditoIds"));
                    if (idsError is not null)
                        return idsError;

                    // Misma resolución que rpt-cobro-diario: cartera del gestor de la caja,
                    // no solo el usuario del JWT (CAJA CENTRAL / admin operando otra caja).
                    var (uid, oid, accessErr) = CobroDiarioReportAccess.ResolveFiltros(
                        httpContext,
                        body.UsuarioId,
                        body.OficinaId,
                        requiereGestor: true);
                    if (accessErr is not null)
                        return accessErr;

                    if (uid is null or < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Parámetros inválidos",
                            detail: "Seleccione un gestor (usuarioId >= 1).");
                    }

                    if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:oficina_id.");
                    }

                    var oficinaRuta = oid is > 0 ? oid.Value : jwtOficinaId;

                    var log = loggerFactory.CreateLogger("GenerarRutaCobros");
                    try
                    {
                        var response = await rutaCobros
                            .GenerarAsync(
                                uid.Value,
                                oficinaRuta,
                                body.CreditoIds ?? Array.Empty<int>(),
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(response);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("GenerarRutaCobros")
            .WithTags("credito-config")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<GenerarRutaCobrosResponse>();

        app.MapGet(
                "/api/v1/credito/ruta-wa/{id}",
                (string id, IRutaCobrosService rutaCobros) =>
                {
                    var texto = rutaCobros.ObtenerTextoRuta(id);
                    if (string.IsNullOrEmpty(texto))
                    {
                        return Results.Content(
                            "Esta ruta ha expirado. Por favor, vuelva a generarla en la computadora de la oficina.",
                            "text/plain; charset=utf-8");
                    }

                    var link = "https://wa.me/?text=" + Uri.EscapeDataString(texto);
                    return Results.Redirect(link);
                })
            .WithName("RutaWa")
            .WithTags("credito-config")
            .AllowAnonymous();
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

        log.LogError(ex, "Error en credito-config");
        var detail = "No se pudo completar la operación.";
        if (env.IsDevelopment())
            detail += $" Detalle: {ex.Message}";
        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }

    private static bool TryNormalizeFactor(string? raw, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "factorVariable y factorFijo son obligatorios.";
            return false;
        }

        var valueText = raw.Trim().Replace(',', '.');
        if (!decimal.TryParse(
                valueText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value))
        {
            error = "factorVariable y factorFijo deben ser valores numericos.";
            return false;
        }

        if (value <= 0 || value > 1_000_000m)
        {
            error = "factorVariable y factorFijo deben ser mayores que cero y menores o iguales a 1000000.";
            return false;
        }

        normalized = value.ToString("0.#############################", CultureInfo.InvariantCulture);
        return true;
    }
}
