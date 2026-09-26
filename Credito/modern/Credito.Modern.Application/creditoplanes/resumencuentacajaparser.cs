using System.Globalization;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Interpreta textos de resumen por tipo de pago:
/// <c>"EFECTIVO = 1065.00  YAPE = 500.00"</c> o
/// <c>"RESUMEN BOVEDA: EFECTIVO = 100.00  YAPE = 50.00"</c>
/// (<c>ufnResumenCuentaCajaDiario</c>, <c>usp_RptSaldosCajaResumenTipoCuenta</c>,
/// <c>usp_ResumenCuentaBoveda</c>).
/// </summary>
public static class ResumenCuentaCajaParser
{
    private static readonly Regex PairRegex = new(
        @"([A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ0-9\s./-]*?)\s*=\s*(-?\d+(?:[.,]\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public readonly record struct Item(string Cuenta, decimal Importe);

    /// <summary>Totales para cabecera de informe: efectivo vs resto (Yape, Plin, bancos…).</summary>
    public readonly record struct Composicion(
        decimal Efectivo,
        decimal MediosDigitales,
        IReadOnlyList<Item> Items);

    public static IReadOnlyList<Item> Parse(string? resumen)
    {
        var texto = resumen?.Trim();
        if (string.IsNullOrEmpty(texto))
        {
            return Array.Empty<Item>();
        }

        // Prefijos de SP: "RESUMEN CAJA DIARIO: …", "RESUMEN BOVEDA: …", "RESUMEN CUENTA: …"
        texto = Regex.Replace(
            texto,
            @"^RESUMEN\s+(?:CAJA\s+DIARIO|BOVEDA|CUENTA)\s*:\s*",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var items = new List<Item>();
        foreach (Match match in PairRegex.Matches(texto))
        {
            var cuenta = match.Groups[1].Value.Trim();
            var bruto = match.Groups[2].Value.Trim().Replace(',', '.');
            if (cuenta.Length == 0
                || !decimal.TryParse(bruto, NumberStyles.Any, CultureInfo.InvariantCulture, out var importe))
            {
                continue;
            }

            items.Add(new Item(cuenta, importe));
        }

        return items;
    }

    public static Composicion Compose(string? resumen)
    {
        var items = AclararParesCentralHuanta(Parse(resumen));
        decimal efectivo = 0m;
        decimal digitales = 0m;
        foreach (var item in items)
        {
            if (EsEfectivo(item.Cuenta))
                efectivo += item.Importe;
            else
                digitales += item.Importe;
        }

        return new Composicion(efectivo, digitales, items);
    }

    public static bool EsEfectivo(string? cuenta)
    {
        if (string.IsNullOrWhiteSpace(cuenta))
            return false;
        var k = cuenta.Trim().ToUpperInvariant();
        return k is "EFECTIVO" or "CASH" || k.Contains("EFECTIVO", StringComparison.Ordinal);
    }

    public static string FormatLine(string? resumen, CultureInfo culture)
    {
        var items = Parse(resumen);
        if (items.Count == 0)
        {
            return resumen?.Trim() ?? string.Empty;
        }

        return string.Join(" · ", items.Select(i => $"{i.Cuenta} {i.Importe.ToString("N2", culture)}"));
    }

    public static string FormatComposicion(string? resumen, CultureInfo culture)
    {
        var c = Compose(resumen);
        if (c.Items.Count == 0)
            return string.Empty;

        return $"Efectivo {c.Efectivo.ToString("N2", culture)} · Medios digitales {c.MediosDigitales.ToString("N2", culture)}";
    }

    /// <summary>
    /// Etiqueta legible conservando sede (Central / Huanta) cuando el SP la distingue.
    /// </summary>
    public static string DisplayLabel(string? cuenta)
    {
        if (string.IsNullOrWhiteSpace(cuenta))
            return string.Empty;

        var k = cuenta.Trim().ToUpperInvariant();
        var sede = k.Contains("HUANTA", StringComparison.Ordinal) ? " Huanta"
            : k.Contains("CENTRAL", StringComparison.Ordinal) ? " Central"
            : string.Empty;

        if (k.Contains("EFECTIVO", StringComparison.Ordinal) || k is "CASH")
            return "Efectivo";
        if (k.Contains("YAPE", StringComparison.Ordinal))
            return "Yape" + sede;
        if (k.Contains("PLIN", StringComparison.Ordinal))
            return "Plin" + sede;
        if (k.Contains("INTERBANK", StringComparison.Ordinal))
            return "Interbank" + sede;
        if (k.Contains("BCP", StringComparison.Ordinal)
            || k.Contains("BCO CREDITO", StringComparison.Ordinal)
            || k.Contains("BANCO CREDITO", StringComparison.Ordinal))
            return "BCP" + sede;
        if (k.Contains("NACION", StringComparison.Ordinal))
            return "Banco de la Nación";
        if (k.Contains("BBVA", StringComparison.Ordinal))
            return "BBVA" + sede;
        if (k.Contains("SCOTIA", StringComparison.Ordinal))
            return "Scotiabank" + sede;

        return CultureInfo.GetCultureInfo("es-PE").TextInfo.ToTitleCase(cuenta.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Aclara pares «YAPE» + «YAPE HUANTA» del SP como Central / Huanta (el SP no escribe CENTRAL).
    /// </summary>
    public static IReadOnlyList<Item> AclararParesCentralHuanta(IReadOnlyList<Item> items)
    {
        if (items.Count == 0)
            return items;

        var marcasHuanta = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var k = item.Cuenta.Trim().ToUpperInvariant();
            if (!k.Contains("HUANTA", StringComparison.Ordinal))
                continue;
            var marca = MarcaBase(k);
            if (marca is not null)
                marcasHuanta.Add(marca);
        }

        if (marcasHuanta.Count == 0)
            return items;

        var result = new List<Item>(items.Count);
        foreach (var item in items)
        {
            var k = item.Cuenta.Trim().ToUpperInvariant();
            if (k.Contains("HUANTA", StringComparison.Ordinal) || k.Contains("CENTRAL", StringComparison.Ordinal))
            {
                result.Add(item);
                continue;
            }

            var marca = MarcaBase(k);
            if (marca is null || !marcasHuanta.Contains(marca))
            {
                result.Add(item);
                continue;
            }

            // Conserva la denominación original del SP + CENTRAL para DisplayLabel.
            result.Add(new Item(item.Cuenta.TrimEnd() + " CENTRAL", item.Importe));
        }

        return result;
    }

    private static string? MarcaBase(string k)
    {
        if (k.Contains("YAPE", StringComparison.Ordinal))
            return "YAPE";
        if (k.Contains("PLIN", StringComparison.Ordinal))
            return "PLIN";
        if (k.Contains("INTERBANK", StringComparison.Ordinal))
            return "INTERBANK";
        if (k.Contains("BCO CREDITO", StringComparison.Ordinal)
            || k.Contains("BANCO CREDITO", StringComparison.Ordinal)
            || k is "BCP"
            || (k.Contains("BCP", StringComparison.Ordinal) && !k.Contains("NACION", StringComparison.Ordinal)))
            return "BCP";
        if (k.Contains("BBVA", StringComparison.Ordinal))
            return "BBVA";
        if (k.Contains("SCOTIA", StringComparison.Ordinal))
            return "SCOTIABANK";
        return null;
    }
}
