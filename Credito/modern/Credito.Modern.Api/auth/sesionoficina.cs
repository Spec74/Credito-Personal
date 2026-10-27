using Credito.Modern.Application.Oficinas;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Credito.Modern.Api.Auth;

internal sealed record SesionOficinaInfo(
    int OficinaId,
    bool EsCentral,
    string Oficina,
    IReadOnlyList<OficinaListItemDto> Destinos);

internal static class SesionOficina
{
    public static async Task<(SesionOficinaInfo? Info, ProblemHttpResult? Error)> ResolverAsync(
        HttpContext http,
        IOficinaReadService oficinas,
        int? sedeId,
        CancellationToken ct)
    {
        if (!MenuIdentity.TryGetOficinaIdFromJwt(http.User, out var oficinaId))
        {
            return (null, TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud inválida",
                detail: "El token no contiene una oficina válida (vendix:oficina_id)."));
        }

        var activas = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
        var sesion = activas.FirstOrDefault(o => o.OficinaId == oficinaId);
        var esCentral = sesion?.IndPrincipal == true;
        var nombre = sesion?.Denominacion?.Trim() ?? string.Empty;

        IReadOnlyList<OficinaListItemDto> destinos;
        if (!esCentral)
        {
            destinos =
            [
                sesion ?? new OficinaListItemDto
                {
                    OficinaId = oficinaId,
                    Denominacion = nombre,
                    IndPrincipal = false,
                    Estado = true,
                },
            ];
        }
        else if (sedeId is >= 1)
        {
            destinos = activas.Where(o => o.OficinaId == sedeId.Value).ToList();
        }
        else
        {
            destinos = activas;
        }

        return (new SesionOficinaInfo(oficinaId, esCentral, nombre, destinos), null);
    }

    /// <summary>
    /// Lectura: sucursal = JWT. Central = <paramref name="sedeId"/> o, si <paramref name="allowRed"/>, null (toda la red).
    /// </summary>
    public static int? OficinaLectura(SesionOficinaInfo info, int? sedeId, bool allowRed)
    {
        if (!info.EsCentral)
        {
            return info.OficinaId;
        }

        if (sedeId is >= 1)
        {
            return sedeId;
        }

        return allowRed ? null : info.OficinaId;
    }

    public static ProblemHttpResult? RechazarSiSucursalAjena(SesionOficinaInfo info, int oficinaDestino)
    {
        if (!info.EsCentral && oficinaDestino != info.OficinaId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Prohibido",
                detail: "No puede operar la sede ajena. Entre a esa oficina para continuar.");
        }

        return null;
    }
}
