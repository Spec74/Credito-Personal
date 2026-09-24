using Credito.Modern.Application.UsuariosAdmin;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Nombre del asesor/gestor que genera el plan del simulador.
/// Prioriza el nombre completo del usuario autenticado (JWT).
/// </summary>
public static class RptSimuladorPlanPagosAsesor
{
    public static async Task<string> ResolveNombreAsync(
        int usuarioId,
        string? asesorQuery,
        IUsuarioAdminReadService usuarios,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId > 0)
        {
            var detalle = await usuarios
                .GetDetalleAsync(usuarioId, cancellationToken)
                .ConfigureAwait(false);
            if (detalle is not null)
            {
                var nombre = FirstNonEmpty(detalle.NombreCompleto, detalle.NombreUsuario);
                if (nombre is not null)
                {
                    return nombre;
                }
            }
        }

        return FirstNonEmpty(asesorQuery) ?? "-";
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
