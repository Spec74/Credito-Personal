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
        var items = Parse(resumen);
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
}
