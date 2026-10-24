namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Reglas de rechazo de solicitud (bandeja PEN). No inventa cuotas ni %:
/// solo decide si se puede borrar la solicitud o hay que anular.
/// </summary>
public static class CreditoRechazoReglas
{
    public const string EstadoPendiente = "PEN";

    public static string? MensajeSiNoRechazable(string? estado, bool tieneMovimientosFinancieros)
    {
        if (!EsPendiente(estado))
        {
            return "Solo se puede rechazar un crédito pendiente de aprobación (PEN). Si ya está aprobado o desembolsado, use anular.";
        }

        if (tieneMovimientosFinancieros)
        {
            return "No se puede rechazar: el crédito ya tiene cuentas por cobrar, pagos de caja o mora. Use anular.";
        }

        return null;
    }

    public static string MensajeDesdeDb(Exception ex)
    {
        var raw = (ex.InnerException?.Message ?? ex.Message) ?? string.Empty;
        if (raw.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("conflicted with the REFERENCE", StringComparison.OrdinalIgnoreCase))
        {
            return "No se pudo rechazar: el crédito tiene registros relacionados que impiden eliminarlo.";
        }

        return "No se pudo rechazar el crédito.";
    }

    public static bool EsPendiente(string? estado) =>
        string.Equals(estado?.Trim(), EstadoPendiente, StringComparison.OrdinalIgnoreCase);
}
