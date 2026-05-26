using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.UsuariosAdmin;

namespace Credito.Modern.Application.CreditoPlanes;

public static class CobranzaPagosExportLabels
{
    public static async Task<(string GestorNombre, string OficinaNombre)> ResolveAsync(
        int? usuarioId,
        int? oficinaId,
        IUsuarioAdminReadService usuarios,
        IOficinaReadService oficinas,
        CancellationToken cancellationToken)
    {
        var gestorNombre = "Todos";
        var oficinaNombre = "Todas";

        if (usuarioId is > 0)
        {
            var detalle = await usuarios.GetDetalleAsync(usuarioId.Value, cancellationToken).ConfigureAwait(false);
            if (detalle is not null)
            {
                gestorNombre = string.IsNullOrWhiteSpace(detalle.NombreCompleto)
                    ? detalle.NombreUsuario
                    : detalle.NombreCompleto;
            }
        }

        if (oficinaId is > 0)
        {
            var activas = await oficinas.GetActivasAsync(cancellationToken).ConfigureAwait(false);
            oficinaNombre = activas.Find(o => o.OficinaId == oficinaId.Value)?.Denominacion
                ?? $"Oficina #{oficinaId.Value}";
        }

        return (gestorNombre, oficinaNombre);
    }
}
