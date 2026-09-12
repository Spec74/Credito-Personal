using Credito.Modern.Api.Auth;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

/// <summary>Validación JWT para informes por gestor (paridad MVC: oficina sesión, gestor opcional TODOS solo roles elevados).</summary>
internal static class GestorInformeReportAccess
{
    /// <summary>Resuelve oficina y gestor según JWT y roles (delega en <see cref="CobroDiarioReportAccess.ResolveFiltros"/>).</summary>
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
            requiereGestor: false);

        if (error is not null)
        {
            return (0, null, error);
        }

        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
        {
            return (0, null, Forbidden("El token no contiene una oficina válida (vendix:oficina_id)."));
        }

        // El índice SPA fija la oficina de sesión; el token no autoriza otra.
        if (oficinaId is not null && oficinaId.Value != jwtOficinaId)
        {
            return (0, null, Forbidden("oficinaId debe coincidir con la oficina del token JWT."));
        }

        return (oficinaId ?? jwtOficinaId, usuarioId, null);
    }

    /// <summary>
    /// Oficina = JWT. Gestor: TODOS o uno concreto si el rol es ADMIN/APROBADOR/PARCIAL;
    /// si no, solo el usuario del token.
    /// </summary>
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
