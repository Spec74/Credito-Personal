namespace Credito.Modern.Application.CreditoTareas;

/// <summary>
/// Extracción de términos para búsqueda de créditos (paridad autocomplete MVC / ClienteBuscar).
/// </summary>
public static class TareaBusquedaTerminos
{
    public static IReadOnlyList<string> ExtraerDesdeEntrada(string term)
    {
        var t = term.Trim();
        if (t.Length == 0)
        {
            return Array.Empty<string>();
        }

        var outList = new List<string>();
        void Push(string s)
        {
            var x = s.Trim();
            if (x.Length >= 2 && !outList.Any(o => string.Equals(o, x, StringComparison.OrdinalIgnoreCase)))
            {
                outList.Add(x);
            }
        }

        Push(t);

        var dniLead = System.Text.RegularExpressions.Regex.Match(t, @"^(\d{6,12})\b");
        if (dniLead.Success)
        {
            Push(dniLead.Groups[1].Value);
        }

        var bracket = System.Text.RegularExpressions.Regex.Match(t, @"\[([^\]]+)\]");
        if (bracket.Success)
        {
            Push(bracket.Groups[1].Value);
        }

        var code = CodigoEnEtiqueta(t);
        if (code != null)
        {
            Push(code);
        }

        var first = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (first != null && System.Text.RegularExpressions.Regex.IsMatch(first, @"^\d{6,12}$"))
        {
            Push(first);
        }

        var numericPrefix = System.Text.RegularExpressions.Regex.Match(t, @"^\d+\s*").Value;
        var namePart = string.IsNullOrEmpty(numericPrefix)
            ? t
            : t.Replace(numericPrefix, string.Empty);
        namePart = namePart.Trim();
        var nameClean = System.Text.RegularExpressions.Regex.Replace(namePart, @"\s*\[[^\]]+\]\s*$", string.Empty).Trim();
        if (nameClean.Length >= 3 && !string.Equals(nameClean, t, StringComparison.OrdinalIgnoreCase))
        {
            Push(nameClean);
        }

        return outList;
    }

    public static string? CodigoEnEtiqueta(string label)
    {
        var m = System.Text.RegularExpressions.Regex.Match(label, @"\[([^\]]+)\]\s*$");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    public static string ClaveOrdenCliente(string nombre, string dni) =>
        $"{nombre.Trim().ToUpperInvariant()}|{dni.Trim()}";
}
