using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Api.Credito;
using Credito.Modern.Application.Clientes;
using Credito.Modern.Application.Integraciones;
using Credito.Modern.Application.Time;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Clientes;

internal static class ClienteEndpoints
{
    public static void MapClienteExtensionEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/clientes/listar",
                async Task<Results<Ok<ClienteListadoResultDto>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    string? buscar,
                    int? page,
                    int? pageSize,
                    string? sortField,
                    string? sortDir,
                    IClienteListadoReadService listado,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var usuarioId))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Prohibido",
                            detail: "El token debe incluir vendix:usuario_id.");
                    }

                    var log = loggerFactory.CreateLogger("ClientesListar");
                    try
                    {
                        var result = await listado
                            .ListarAsync(
                                usuarioId,
                                buscar,
                                page ?? 1,
                                pageSize ?? 15,
                                sortField ?? "Codigo",
                                sortDir ?? "asc",
                                ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ClienteReadError(log, env, ex, "listar clientes");
                    }
                })
            .WithName("ClientesListar")
            .WithSummary(
                "Grilla de clientes (paridad ClienteBL.LstClienteJGrid): sin búsqueda = créditos del usuario; con búsqueda = catálogo.")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<ClienteListadoResultDto>();

        app.MapGet(
                "/api/v1/clientes/por-documento",
                async Task<Results<Ok<PersonaPorDocumentoDto>, NotFound, ProblemHttpResult>> (
                    string? documento,
                    IClienteDetalleReadService clientes,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ClientesPorDocumento");
                    try
                    {
                        var row = await clientes
                            .ObtenerPersonaPorDocumentoAsync(documento ?? string.Empty, ct)
                            .ConfigureAwait(false);
                        if (row is null)
                        {
                            return TypedResults.NotFound();
                        }

                        return TypedResults.Ok(row);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ClienteReadError(log, env, ex, "obtener persona por documento");
                    }
                })
            .WithName("ClientesPorDocumento")
            .WithSummary("Paridad ClienteController.ObtenerClienteDNI (precarga alta).")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<PersonaPorDocumentoDto>()
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet(
                "/api/v1/clientes/distritos-buscar",
                async Task<Results<Ok<List<ClienteBuscarItemDto>>, ProblemHttpResult>> (
                    string? term,
                    IClienteAuxReadService aux,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ClientesDistritosBuscar");
                    try
                    {
                        var items = await aux.BuscarDistritosAsync(term ?? string.Empty, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ClienteReadError(log, env, ex, "buscar distritos");
                    }
                })
            .WithName("ClientesDistritosBuscar")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet(
                "/api/v1/clientes/personas-buscar",
                async Task<Results<Ok<List<ClienteBuscarItemDto>>, ProblemHttpResult>> (
                    string? term,
                    IClienteAuxReadService aux,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ClientesPersonasBuscar");
                    try
                    {
                        var items = await aux.BuscarPersonasAsync(term ?? string.Empty, ct).ConfigureAwait(false);
                        return TypedResults.Ok(items.ToList());
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ClienteReadError(log, env, ex, "buscar personas");
                    }
                })
            .WithName("ClientesPersonasBuscar")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet(
                "/api/v1/integraciones/apiperu/dni/{dni}",
                async Task<Results<Ok<ApiPeruDniDto>, ProblemHttpResult>> (
                    string dni,
                    IApiPeruService apiPeru,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ApiPeruDni");
                    try
                    {
                        return TypedResults.Ok(await apiPeru.ConsultarDniAsync(dni, ct).ConfigureAwait(false));
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
                    {
                        log.LogWarning(ex, "ApiPeru DNI");
                        return TypedResults.Ok(
                            new ApiPeruDniDto(
                                false,
                                null,
                                null,
                                null,
                                "Servicio de validación no disponible. Ingrese los datos manualmente y verifique la configuración de API Perú."));
                    }
                })
            .WithName("ApiPeruConsultarDni")
            .WithTags("clientes", "integraciones")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapGet(
                "/api/v1/integraciones/apiperu/ruc/{ruc}",
                async Task<Results<Ok<ApiPeruRucDto>, ProblemHttpResult>> (
                    string ruc,
                    IApiPeruService apiPeru,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ApiPeruRuc");
                    try
                    {
                        return TypedResults.Ok(await apiPeru.ConsultarRucAsync(ruc, ct).ConfigureAwait(false));
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
                    {
                        log.LogWarning(ex, "ApiPeru RUC");
                        return TypedResults.Ok(
                            new ApiPeruRucDto(
                                false,
                                null,
                                null,
                                "Servicio de validación no disponible. Ingrese los datos manualmente y verifique la configuración de API Perú."));
                    }
                })
            .WithName("ApiPeruConsultarRuc")
            .WithTags("clientes", "integraciones")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost(
                "/api/v1/clientes/crear-persona-rapida",
                async Task<Results<Ok<CrearPersonaRapidaResponse>, ProblemHttpResult>> (
                    HttpContext httpContext,
                    CrearPersonaRapidaRequest body,
                    IClienteWriteService write,
                    IDatabaseTimeProvider databaseTime,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var usuarioError = CajaCreditoWriteGuards.ValidateJwtUsuario(httpContext, out var usuarioId);
                    if (usuarioError is not null)
                    {
                        return usuarioError;
                    }

                    var log = loggerFactory.CreateLogger("ClientesCrearPersonaRapida");
                    try
                    {
                        var serverTime = await databaseTime.GetServerTimeAsync(ct).ConfigureAwait(false)
                            ?? DateTime.Now;
                        var result = await write
                            .CrearPersonaRapidaAsync(body, usuarioId, serverTime, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ClienteReadError(log, env, ex, "crear persona rápida");
                    }
                })
            .WithName("ClientesCrearPersonaRapida")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);

        app.MapPost(
                "/api/v1/clientes/{personaId:int}/habilitar-depurado",
                async Task<Results<Ok<bool>, ProblemHttpResult>> (
                    int personaId,
                    IClienteWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("ClientesHabilitarDepurado");
                    try
                    {
                        var ok = await write.HabilitarDepuradoAsync(personaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(ok);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ClienteReadError(log, env, ex, "habilitar depurado");
                    }
                })
            .WithName("ClientesHabilitarDepurado")
            .WithTags("clientes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser);
    }

    private static ProblemHttpResult ClienteReadError(ILogger log, IHostEnvironment env, Exception ex, string accion)
    {
        if (ex is InvalidOperationException ioe)
        {
            log.LogWarning(ioe, "Clientes {Accion}: configuración", accion);
            return TypedResults.Problem(
                detail: "No se pudo completar la operación por configuración incompleta del servidor.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Configuración incompleta");
        }

        log.LogError(ex, "Clientes {Accion}", accion);
        var detail = $"No se pudo {accion}.";
        if (env.IsDevelopment())
        {
            detail += $" Detalle: {ex.Message}";
        }

        return TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Error de base de datos");
    }
}
