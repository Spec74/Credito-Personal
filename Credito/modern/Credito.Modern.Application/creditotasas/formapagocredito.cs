namespace Credito.Modern.Application.CreditoTasas;

/// <summary>Códigos de modalidad que envía el MVC a <c>usp_CalcularTEM</c> (p. ej. <c>cboFormaPago</c> en <c>Creditos.cshtml</c>).</summary>
public static class FormaPagoCredito
{
    /// <summary>D=diario, M=mensual, Q=quincenal, S=semanal.</summary>
    public const string CodigosPermitidos = "DMQS";

    public static bool TryNormalizar(string? raw, out string normalizado, out string mensajeError)
    {
        normalizado = string.Empty;
        mensajeError = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            mensajeError = "formaPago es obligatoria.";
            return false;
        }

        var t = raw.Trim();
        if (t.Length != 1)
        {
            mensajeError =
                "formaPago debe ser una sola letra: D (diario), M (mensual), Q (quincenal), S (semanal), igual que en el legado VENDIX.";
            return false;
        }

        var c = char.ToUpperInvariant(t[0]);
        if (CodigosPermitidos.IndexOf(c) < 0)
        {
            mensajeError =
                "formaPago no reconocida. Use D, M, Q o S (mayúsculas o minúsculas).";
            return false;
        }

        normalizado = c.ToString();
        return true;
    }
}
