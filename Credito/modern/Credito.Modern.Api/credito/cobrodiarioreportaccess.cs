using System.Security.Claims;
using Credito.Modern.Api.Auth;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Credito;

internal static class CobroDiarioReportAccess
{
    private static readonly string[] ElevatedRoles =
        ["ADMIN", "APROBADOR", "PARCIAL", "ADMINISTRADOR"];

    /// <summary>Roles que en MVC pueden elegir gestor/oficina en reportes de crédito (p. ej. CobranzaPagos).</summary>
    internal static bool TieneRolReporteCredito(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            var trimmed = (role ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (ElevatedRoles.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.StartsWith("APROBADOR", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Paridad <c>Reporte/ObtenerCobranzaPagos</c>: gestor y oficina opcionales (null = TODOS).</summary>
    internal static (int? UsuarioId, int? OficinaId, ProblemHttpResult? Error) ResolveCobranzaFiltros(
        HttpContext httpContext,
        int? usuarioId,
        int? oficinaId)
    {
        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
        {
            return (null, null, Forbidden("El token no contiene una oficina válida (vendix:oficina_id)."));
        }

        if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var jwtUsuarioId))
        {
            return (null, null, Forbidden("El token no contiene un usuario válido (vendix:usuario_id)."));
        }

        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (TieneRolReporteCredito(roles))
        {
            if (usuarioId is 0)
            {
                return (null, null, BadRequest("usuarioId no puede ser 0."));
            }

            if (oficinaId is 0)
            {
                return (null, null, BadRequest("oficinaId no puede ser 0."));
            }

            var uid = usuarioId is null or < 1 ? null : usuarioId;
            var oid = oficinaId is null or < 1 ? null : oficinaId;
            return (uid, oid, null);
        }

        if (usuarioId is not null && usuarioId != jwtUsuarioId)
        {
            return (null, null, Forbidden("usuarioId debe coincidir con el usuario del token JWT."));
        }

        if (oficinaId is not null && oficinaId != jwtOficinaId)
        {
            return (null, null, Forbidden("oficinaId debe coincidir con la oficina del token JWT."));
        }

        return (jwtUsuarioId, jwtOficinaId, null);
    }

    internal static (int? UsuarioId, int? OficinaId, ProblemHttpResult? Error) ResolveFiltros(
        HttpContext httpContext,
        int? usuarioId,
        int? oficinaId,
        bool requiereGestor = true)
    {
        if (!MenuIdentity.TryGetOficinaIdFromJwt(httpContext.User, out var jwtOficinaId))
        {
            return (null, null, Forbidden("El token no contiene una oficina válida (vendix:oficina_id)."));
        }

        if (!MenuIdentity.TryGetUsuarioIdFromJwt(httpContext.User, out var jwtUsuarioId))
        {
            return (null, null, Forbidden("El token no contiene un usuario válido (vendix:usuario_id)."));
        }

        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var elevated = TieneRolReporteCredito(roles);

        if (elevated)
        {
            if (requiereGestor && usuarioId is null or < 1)
            {
                return (null, null, BadRequest("Seleccione un gestor (usuarioId >= 1)."));
            }

            if (usuarioId is 0)
            {
                return (null, null, BadRequest("usuarioId no puede ser 0."));
            }

            if (oficinaId is 0)
            {
                return (null, null, BadRequest("oficinaId no puede ser 0."));
            }

            var uidElevated = usuarioId is null or < 1 ? null : usuarioId;
            var oidElevated = oficinaId is null or < 1 ? null : oficinaId;
            return (uidElevated, oidElevated, null);
        }

        var uid = usuarioId is null or < 1 ? jwtUsuarioId : usuarioId.Value;
        var oid = oficinaId is null or < 1 ? jwtOficinaId : oficinaId.Value;

        if (requiereGestor && usuarioId is null or < 1)
        {
            return (null, null, BadRequest("Seleccione un gestor (usuarioId >= 1)."));
        }

        if (usuarioId is not null and > 0 && uid != jwtUsuarioId)
        {
            return (null, null, Forbidden("usuarioId debe coincidir con el usuario del token JWT."));
        }

        if (oficinaId is not null and > 0 && oid != jwtOficinaId)
        {
            return (null, null, Forbidden("oficinaId debe coincidir con la oficina del token JWT."));
        }

        return (uid, oid, null);
    }

    internal static List<RptCobroDiarioRowDto> ApplySoloMora(
        IReadOnlyList<RptCobroDiarioRowDto> items,
        bool soloMora) =>
        soloMora
            ? items.Where(x => (x.Mora ?? 0) > 0).ToList()
            : items.ToList();

    private static ProblemHttpResult BadRequest(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Parámetros inválidos",
            detail: detail);

    private static ProblemHttpResult Forbidden(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Prohibido",
            detail: detail);
}
