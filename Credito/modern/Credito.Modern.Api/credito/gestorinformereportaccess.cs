using Credito.Modern.Api.Auth;
using Credito.Modern.Application.Oficinas;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

/// <summary>
/// Validación JWT para informes por gestor.
/// Central puede consultar otra sede vía <c>oficinaId</c>; sucursal queda fijada al token.
/// </summary>
internal static class GestorInformeReportAccess
{
    /// <summary>Oficina obligatoria (≥ 1). Central respeta la sede pedida.</summary>
    internal static async Task<(int OficinaId, int? UsuarioId, ProblemHttpResult? Error)> ResolveAsync(
        HttpContext httpContext,
        IOficinaReadService oficinas,
        int? oficinaIdQuery,
        int? usuarioIdQuery,
        CancellationToken ct)
    {
        if (oficinaIdQuery is null or < 1)
        {
            return (0, null, BadRequest("oficinaId es obligatorio y debe ser un entero >= 1."));
        }

        if (usuarioIdQuery is 0)
        {
            return (0, null, BadRequest("usuarioId no puede ser 0."));
        }

        if (usuarioIdQuery is < 0)
        {
            return (0, null, BadRequest("usuarioId, si se indica, debe ser un entero >= 1."));
        }

        var (info, err) = await SesionOficina.ResolverAsync(httpContext, oficinas, oficinaIdQuery, ct)
            .ConfigureAwait(false);
        if (err is not null)
        {
            return (0, null, err);
        }

        var (usuarioId, oficinaId, error) = CobroDiarioReportAccess.ResolveFiltros(
            httpContext,
            usuarioIdQuery,
            oficinaIdQuery,
            requiereGestor: false,
            esCentral: info!.EsCentral);

        if (error is not null)
        {
            return (0, null, error);
        }

        var oid = SesionOficina.OficinaLectura(info, oficinaIdQuery, allowRed: false)
            ?? info.OficinaId;
        if (oficinaId is >= 1)
        {
            oid = oficinaId.Value;
        }

        if (!info.EsCentral && oid != info.OficinaId)
        {
            return (0, null, Forbidden("oficinaId debe coincidir con la oficina del token JWT."));
        }

        return (oid, usuarioId, null);
    }

    /// <summary>
    /// Oficina opcional (null = todas). Solo Central puede omitir o cruzar sede.
    /// </summary>
    internal static async Task<(int? OficinaId, int? UsuarioId, ProblemHttpResult? Error)> ResolveOptionalOficinaAsync(
        HttpContext httpContext,
        IOficinaReadService oficinas,
        int? oficinaIdQuery,
        int? usuarioIdQuery,
        CancellationToken ct)
    {
        if (oficinaIdQuery is 0)
        {
            return (null, null, BadRequest("oficinaId no puede ser 0."));
        }

        if (usuarioIdQuery is 0)
        {
            return (null, null, BadRequest("usuarioId no puede ser 0."));
        }

        if (usuarioIdQuery is < 0)
        {
            return (null, null, BadRequest("usuarioId, si se indica, debe ser un entero >= 1."));
        }

        var (info, err) = await SesionOficina.ResolverAsync(httpContext, oficinas, oficinaIdQuery, ct)
            .ConfigureAwait(false);
        if (err is not null)
        {
            return (null, null, err);
        }

        var (usuarioId, oficinaId, error) = CobroDiarioReportAccess.ResolveFiltros(
            httpContext,
            usuarioIdQuery,
            oficinaIdQuery is null or < 1 ? null : oficinaIdQuery,
            requiereGestor: false,
            esCentral: info!.EsCentral);

        if (error is not null)
        {
            return (null, null, error);
        }

        if (!info.EsCentral)
        {
            return (info.OficinaId, usuarioId, null);
        }

        return (oficinaId, usuarioId, null);
    }

    /// <summary>
    /// Valida <c>oficinaId</c> de consulta: sucursal = JWT; Central puede elegir sede.
    /// </summary>
    internal static async Task<(int OficinaId, ProblemHttpResult? Error)> EnsureOficinaConsultaAsync(
        HttpContext httpContext,
        IOficinaReadService oficinas,
        int? oficinaId,
        CancellationToken ct)
    {
        if (oficinaId is null or < 1)
        {
            return (0, BadRequest("oficinaId es obligatorio y debe ser un entero >= 1."));
        }

        var (info, err) = await SesionOficina.ResolverAsync(httpContext, oficinas, oficinaId, ct)
            .ConfigureAwait(false);
        if (err is not null)
        {
            return (0, err);
        }

        if (!info!.EsCentral && oficinaId.Value != info.OficinaId)
        {
            return (0, Forbidden("oficinaId debe coincidir con la oficina del token JWT."));
        }

        return (oficinaId.Value, null);
    }

    /// <summary>Compat sync: asume no-Central (JWT). Preferir <see cref="ResolveAsync"/>.</summary>
    internal static (int OficinaId, int? UsuarioId, ProblemHttpResult? Error) Resolve(
        HttpContext httpContext,
        int? oficinaIdQuery,
        int? usuarioIdQuery)
    {
        if (oficinaIdQuery is null or < 1)
        {
            return (0, null, BadRequest("oficinaId es obligatorio y debe ser un entero >= 1."));
        }

        if (usuarioIdQuery is 0)
        {
            return (0, null, BadRequest("usuarioId no puede ser 0."));
        }

        if (usuarioIdQuery is < 0)
        {
            return (0, null, BadRequest("usuarioId, si se indica, debe ser un entero >= 1."));
        }

        var (usuarioId, oficinaId, error) = CobroDiarioReportAccess.ResolveFiltros(
            httpContext,
            usuarioIdQuery,
            oficinaIdQuery,
            requiereGestor: false,
            esCentral: false);

        if (error is not null)
        {
            return (0, null, error);
        }

        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
        {
            return (0, null, Forbidden("El token no contiene una oficina válida (vendix:oficina_id)."));
        }

        return (oficinaId ?? jwtOficinaId, usuarioId, null);
    }

    internal static (int? OficinaId, int? UsuarioId, ProblemHttpResult? Error) ResolveOptionalOficina(
        HttpContext httpContext,
        int? oficinaIdQuery,
        int? usuarioIdQuery)
    {
        if (oficinaIdQuery is 0)
        {
            return (null, null, BadRequest("oficinaId no puede ser 0."));
        }

        if (usuarioIdQuery is 0)
        {
            return (null, null, BadRequest("usuarioId no puede ser 0."));
        }

        if (usuarioIdQuery is < 0)
        {
            return (null, null, BadRequest("usuarioId, si se indica, debe ser un entero >= 1."));
        }

        var (usuarioId, oficinaId, error) = CobroDiarioReportAccess.ResolveFiltros(
            httpContext,
            usuarioIdQuery,
            oficinaIdQuery is null or < 1 ? null : oficinaIdQuery,
            requiereGestor: false,
            esCentral: false);

        if (error is not null)
        {
            return (null, null, error);
        }

        return (oficinaId, usuarioId, null);
    }

    internal static ProblemHttpResult? Validate(
        HttpContext httpContext,
        int? oficinaId,
        int? usuarioId) =>
        Resolve(httpContext, oficinaId, usuarioId).Error;

    internal static ProblemHttpResult? ValidateOficinaUsuario(
        HttpContext httpContext,
        int oficinaId,
        int? usuarioId) =>
        Resolve(httpContext, oficinaId, usuarioId).Error;

    private static ProblemHttpResult Forbidden(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Prohibido",
            detail: detail);

    private static ProblemHttpResult BadRequest(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Parámetros inválidos",
            detail: detail);
}
