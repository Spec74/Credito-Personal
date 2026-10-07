using Credito.Modern.Application.CreditoPlanes;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MorosidadAccessService(IOptions<MorosidadOptions> options)
    : IMorosidadAccessService
{
    private readonly MorosidadOptions _options = options.Value;

    public bool PuedeConsultar(int usuarioId, IEnumerable<string> roles)
    {
        if (usuarioId < 1)
        {
            return false;
        }

        if (_options.PermitirAdministradores && EsAdministrador(roles))
        {
            return true;
        }

        return ContieneUsuario(_options.UsuarioConsultaIds, usuarioId);
    }

    private static bool EsAdministrador(IEnumerable<string> roles) =>
        roles.Any(r =>
        {
            var u = (r ?? string.Empty).Trim().ToUpperInvariant();
            return u is "ADMIN" or "ADMINISTRADOR";
        });

    private static bool ContieneUsuario(int[]? ids, int usuarioId) =>
        ids is { Length: > 0 } && ids.Contains(usuarioId);
}
