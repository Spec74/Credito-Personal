using System.Globalization;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.CreditoPlanes;

internal readonly record struct CuotaItem(string Monto, string Fecha, bool EsCero);

internal static class CobranzaPagosCuotasParser
{
    internal const int PagosPerRow = 8;

    internal static List<CuotaItem> Parse(string? pagos)
    {
        var list = new List<CuotaItem>();
        if (string.IsNullOrWhiteSpace(pagos))
        {
            return list;
        }

        foreach (var pago in pagos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(pago))
            {
                continue;
            }

            var pagoTrim = pago.Trim();
            var monto = pagoTrim;
            var fecha = "";
            var matchFecha = Regex.Match(pagoTrim, @"\(([^)]+)\)");
            if (matchFecha.Success)
            {
                fecha = matchFecha.Groups[1].Value;
                monto = pagoTrim.Replace(matchFecha.Value, "", StringComparison.Ordinal).Trim();
            }

            var esCero = monto.StartsWith("0.00", StringComparison.Ordinal)
                || monto.StartsWith("0 ", StringComparison.Ordinal)
                || monto == "0";
            list.Add(new CuotaItem(monto, fecha, esCero));
        }

        return list;
    }

    internal static (int Pagos, int Impagos) Count(IReadOnlyList<CuotaItem> items) =>
        (items.Count(p => !p.EsCero), items.Count(p => p.EsCero));
}
