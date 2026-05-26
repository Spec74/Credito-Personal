using System.Globalization;
using System.Security.Claims;

namespace Credito.Modern.Api.Auth;

public static class MenuIdentity
{
    /// <summary>Resuelve oficina y usuario desde claims JWT o desde query (compatibilidad migración).</summary>
    public static bool TryResolve(
        ClaimsPrincipal? user,
        int? oficinaIdQuery,
        int? usuarioIdQuery,
        bool allowQueryParameters,
        out int oficinaId,
        out int usuarioId)
    {
        oficinaId = 0;
        usuarioId = 0;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var o = user.FindFirst(VendixClaims.OficinaId)?.Value;
            var u = user.FindFirst(VendixClaims.UsuarioId)?.Value;
            if (int.TryParse(o, NumberStyles.Integer, CultureInfo.InvariantCulture, out var oi)
                && int.TryParse(u, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui)
                && oi >= 1 && ui >= 1)
            {
                oficinaId = oi;
                usuarioId = ui;
                return true;
            }
        }

        if (allowQueryParameters && oficinaIdQuery is >= 1 && usuarioIdQuery is >= 1)
        {
            oficinaId = oficinaIdQuery.Value;
            usuarioId = usuarioIdQuery.Value;
            return true;
        }

        return false;
    }

    /// <summary>Oficina del JWT (solo usuario autenticado con claim válido).</summary>
    public static bool TryGetOficinaIdFromJwt(ClaimsPrincipal? user, out int oficinaId)
    {
        oficinaId = 0;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var o = user.FindFirst(VendixClaims.OficinaId)?.Value;
        return int.TryParse(o, NumberStyles.Integer, CultureInfo.InvariantCulture, out oficinaId)
            && oficinaId >= 1;
    }

    /// <summary>Usuario del JWT (solo usuario autenticado con claim válido).</summary>
    public static bool TryGetUsuarioIdFromJwt(ClaimsPrincipal? user, out int usuarioId)
    {
        usuarioId = 0;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var u = user.FindFirst(VendixClaims.UsuarioId)?.Value;
        return int.TryParse(u, NumberStyles.Integer, CultureInfo.InvariantCulture, out usuarioId)
            && usuarioId >= 1;
    }
}
