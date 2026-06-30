using System.Data.Common;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.UsuariosAdmin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Admin;

internal static class UsuarioAdminEndpoints
{
    public static void MapUsuarioAdminEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/usuarios/gestion",
                async Task<Results<Ok<UsuarioGestionPageDto>, ProblemHttpResult>> (
                    string? buscar,
                    int? page,
                    int? pageSize,
                    bool? incluirInactivos,
                    IUsuarioAdminReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("UsuariosGestion");
                    try
                    {
                        var result = await usuarios
                            .ListGestionAsync(buscar, page ?? 1, pageSize ?? 25, incluirInactivos == true, ct)
                            .ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("UsuariosGestion")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<UsuarioGestionPageDto>();

        app.MapGet(
                "/api/v1/usuarios/reporte-gestores",
                async Task<Results<Ok<IReadOnlyList<UsuarioReporteGestorDto>>, ProblemHttpResult>> (
                    IUsuarioAdminReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    var log = loggerFactory.CreateLogger("UsuariosReporteGestores");
                    try
                    {
                        var rows = await usuarios.ListReporteGestoresAsync(ct).ConfigureAwait(false);
                        return TypedResults.Ok(rows);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("UsuariosReporteGestores")
            .WithTags("usuarios-admin", "reportes")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoUser)
            .Produces<IReadOnlyList<UsuarioReporteGestorDto>>();

        app.MapGet(
                "/api/v1/usuarios/{usuarioId:int}/detalle",
                async Task<Results<Ok<UsuarioPersonaDetalleDto>, ProblemHttpResult>> (
                    int usuarioId,
                    IUsuarioAdminReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId debe ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("UsuarioDetalle");
                    try
                    {
                        var detalle = await usuarios.GetDetalleAsync(usuarioId, ct).ConfigureAwait(false);
                        if (detalle is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "Usuario no encontrado.");
                        }

                        return TypedResults.Ok(detalle);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("UsuarioDetalle")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<UsuarioPersonaDetalleDto>();

        app.MapGet(
                "/api/v1/usuarios/persona-por-dni",
                async Task<Results<Ok<PersonaPorDniDto>, ProblemHttpResult>> (
                    string dni,
                    IUsuarioAdminReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(dni))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "dni es obligatorio.");
                    }

                    var log = loggerFactory.CreateLogger("PersonaPorDni");
                    try
                    {
                        var persona = await usuarios.GetPersonaPorDniAsync(dni, ct).ConfigureAwait(false);
                        if (persona is null)
                        {
                            return TypedResults.Problem(
                                statusCode: StatusCodes.Status404NotFound,
                                title: "No encontrado",
                                detail: "No hay persona con ese DNI.");
                        }

                        return TypedResults.Ok(persona);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("PersonaPorDni")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<PersonaPorDniDto>();

        app.MapGet(
                "/api/v1/usuarios/validar-dni",
                async Task<Results<Ok<ValidarDniResponse>, ProblemHttpResult>> (
                    string dni,
                    int? usuarioId,
                    IUsuarioAdminReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(dni))
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "dni es obligatorio.");
                    }

                    var log = loggerFactory.CreateLogger("ValidarDni");
                    try
                    {
                        var result = await usuarios.ValidarDniAsync(dni, usuarioId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(result);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("ValidarUsuarioDni")
            .WithSummary("Paridad ValidarUsuarioDNI: existe=true si ya hay usuario con ese DNI.")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<ValidarDniResponse>();

        app.MapGet(
                "/api/v1/usuarios/{usuarioId:int}/roles-asignacion",
                async Task<Results<Ok<List<RolAsignacionDto>>, ProblemHttpResult>> (
                    int usuarioId,
                    int oficinaId,
                    IUsuarioAdminReadService usuarios,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1 || oficinaId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId y oficinaId deben ser >= 1.");
                    }

                    var log = loggerFactory.CreateLogger("UsuarioRolesAsignacion");
                    try
                    {
                        var roles = await usuarios.GetRolesAsignacionAsync(usuarioId, oficinaId, ct).ConfigureAwait(false);
                        return TypedResults.Ok(roles);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or DbException)
                    {
                        return ReadError(log, env, ex);
                    }
                })
            .WithName("UsuarioRolesAsignacion")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<List<RolAsignacionDto>>();

        app.MapPost(
                "/api/v1/usuarios/guardar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    GuardarUsuarioRequest body,
                    IUsuarioAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "GuardarUsuario",
                        ct,
                        token => write.GuardarAsync(body, token)).ConfigureAwait(false);
                })
            .WithName("GuardarUsuario")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/usuarios/{usuarioId:int}/activar",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int usuarioId,
                    IUsuarioAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId debe ser >= 1.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "ActivarUsuario",
                        ct,
                        token => write.ActivarAsync(usuarioId, token)).ConfigureAwait(false);
                })
            .WithName("ActivarUsuario")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/usuarios/{usuarioId:int}/resetear-clave",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int usuarioId,
                    IUsuarioAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId debe ser >= 1.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "ResetearClaveUsuario",
                        ct,
                        token => write.ResetearClaveAsync(usuarioId, token)).ConfigureAwait(false);
                })
            .WithName("ResetearClaveUsuario")
            .WithSummary("Paridad ResetearClave (clave 123456 hasheada para login moderno).")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/usuarios/{usuarioId:int}/asignar-oficinas",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int usuarioId,
                    AsignarOficinasRequest body,
                    IUsuarioAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId debe ser >= 1.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "AsignarOficinasUsuario",
                        ct,
                        token => write.AsignarOficinasAsync(usuarioId, body.OficinaIds, token)).ConfigureAwait(false);
                })
            .WithName("AsignarOficinasUsuario")
            .WithTags("usuarios-admin")
            .RequireAuthorization(CreditoAuthorizationPolicies.CreditoRolAdministrador)
            .Produces<MaestroOperacionResponse>();

        app.MapPost(
                "/api/v1/usuarios/{usuarioId:int}/asignar-roles",
                async Task<Results<Ok<MaestroOperacionResponse>, ProblemHttpResult>> (
                    int usuarioId,
                    AsignarRolesRequest body,
                    IUsuarioAdminWriteService write,
                    ILoggerFactory loggerFactory,
                    IHostEnvironment env,
                    CancellationToken ct) =>
                {
                    if (usuarioId < 1)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Solicitud inválida",
                            detail: "usuarioId debe ser >= 1.");
                    }

                    return await WriteAsync(
                        loggerFactory,
                        env,
                        "AsignarRolesUsuario",
                        ct,
                        token => write.AsignarRolesAsync(usuarioId, body.OficinaId, body.RolIds, token)).ConfigureAwait(false);
                })
            .WithName("AsignarRolesUsuario")
            .WithTags("usuarios-admin")
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

        log.LogError(ex, "Error en usuarios admin");
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

