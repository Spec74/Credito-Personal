using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Paridad <c>ReporteController.ExportarCobranzaPagosExcel</c> (HTML con estilos como .xls, no CSV).
/// </summary>
public static class CobranzaPagosExcelHtmlFormatter
{
    private const int PagosPerRow = 8;

    public static byte[] ToLegacyExcelHtml(
        IReadOnlyList<RptCobroDiarioDetalleRowDto> rows,
        string gestorNombre,
        string oficinaNombre,
        DateTime fechaImpresion)
    {
        var sb = new StringBuilder(Math.Max(4096, rows.Count * 512));
        sb.Append("<html xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
        sb.Append("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
        sb.Append("<style>");
        sb.Append("table { border-collapse: collapse; width: 100%; }");
        sb.Append("th { background-color: #343a40; color: white; font-weight: bold; padding: 8px; border: 1px solid #dee2e6; text-align: center; }");
        sb.Append("td { padding: 6px; border: 1px solid #dee2e6; }");
        sb.Append(".text-right { text-align: right; }");
        sb.Append(".text-center { text-align: center; }");
        sb.Append(".totales { background-color: #343a40; color: white; font-weight: bold; }");
        sb.Append("</style></head><body>");

        sb.Append("<table>");
        sb.Append("<tr><td colspan='10' style='font-size: 16px; font-weight: bold; text-align: center; padding: 10px;'>");
        sb.Append("REPORTE DE COBRANZA POR GESTOR</td></tr>");
        sb.Append("<tr><td colspan='10' style='text-align: center;'>Oficina: ");
        sb.Append(H(oficinaNombre));
        sb.Append(" | Gestor: ");
        sb.Append(H(gestorNombre));
        sb.Append(" | Fecha: ");
        sb.Append(fechaImpresion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
        sb.Append("</td></tr><tr><td colspan='10'>&nbsp;</td></tr></table>");

        sb.Append("<table><thead><tr>");
        foreach (var h in new[]
                 {
                     "Nro", "Cliente", "Tipo", "Crédito", "Interés", "Monto Total", "1er Pago", "Vencimiento",
                     "Total Pagado", "Saldo",
                 })
        {
            sb.Append("<th>").Append(H(h)).Append("</th>");
        }

        sb.Append("</tr></thead><tbody>");

        foreach (var item in rows)
        {
            AppendClienteRow(sb, item);
            AppendPagosRows(sb, item, fechaImpresion);
        }

        sb.Append("<tr class='totales'>");
        sb.Append("<td colspan='3' style='text-align: right;'>TOTALES (").Append(rows.Count).Append(" clientes):</td>");
        sb.Append("<td class='text-right'>").Append(rows.Sum(x => x.MontoCredito).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right'>").Append(rows.Sum(x => x.Interes).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right'>").Append(rows.Sum(x => x.MontoTotal ?? 0).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td colspan='2'></td>");
        sb.Append("<td class='text-right'>").Append(rows.Sum(x => x.TotalPago ?? 0).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right'>").Append(rows.Sum(x => x.Saldo ?? 0).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("</tr></tbody></table></body></html>");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void AppendClienteRow(StringBuilder sb, RptCobroDiarioDetalleRowDto item)
    {
        sb.Append("<tr>");
        sb.Append("<td class='text-center'>").Append(item.Nro?.ToString(CultureInfo.InvariantCulture) ?? "").Append("</td>");
        sb.Append("<td style='font-weight: bold; color: #0066cc;'>").Append(H(item.Cliente)).Append("</td>");
        sb.Append("<td class='text-center'>").Append(H(item.FormaPago)).Append("</td>");
        sb.Append("<td class='text-right'>").Append(item.MontoCredito.ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right'>").Append(item.Interes.ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right'>").Append((item.MontoTotal ?? 0).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-center'>").Append(item.FechaPrimerPago.ToString("d/MM/yyyy", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-center'>").Append(item.FechaVencimiento.ToString("d/MM/yyyy", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right' style='font-weight: bold;'>").Append((item.TotalPago ?? 0).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td class='text-right' style='font-weight: bold; color: #dc3545;'>").Append((item.Saldo ?? 0).ToString("N2", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("</tr>");
    }

    private static void AppendPagosRows(StringBuilder sb, RptCobroDiarioDetalleRowDto item, DateTime fechaImpresion)
    {
        if (string.IsNullOrWhiteSpace(item.Pagos))
        {
            return;
        }

        var pagosValidos = new List<(string Monto, string Fecha, bool EsCero)>();
        foreach (var pago in item.Pagos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
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
            pagosValidos.Add((monto, fecha, esCero));
        }

        for (var rowStart = 0; rowStart < pagosValidos.Count; rowStart += PagosPerRow)
        {
            sb.Append("<tr style='color: #dc3545;'><td></td><td></td>");
            for (var col = 0; col < PagosPerRow; col++)
            {
                var idx = rowStart + col;
                if (idx < pagosValidos.Count)
                {
                    var p = pagosValidos[idx];
                    var bg = p.EsCero ? "#dc3545" : "#17a2b8";
                    sb.Append("<td class='text-center' style='background-color: ").Append(bg);
                    sb.Append("; color: white; font-weight: bold;'>").Append(H(p.Monto)).Append("</td>");
                }
                else
                {
                    sb.Append("<td></td>");
                }
            }

            sb.Append("</tr><tr style='color: #dc3545; font-size: 10px;'><td></td><td></td>");
            for (var col = 0; col < PagosPerRow; col++)
            {
                var idx = rowStart + col;
                if (idx < pagosValidos.Count)
                {
                    var p = pagosValidos[idx];
                    var bg = p.EsCero ? "#dc3545" : "#17a2b8";
                    sb.Append("<td class='text-center' style='background-color: ").Append(bg);
                    sb.Append("; color: white;'>").Append(H(p.Fecha)).Append("</td>");
                }
                else
                {
                    sb.Append("<td></td>");
                }
            }

            sb.Append("</tr>");
        }

        var pagosCount = pagosValidos.Count(p => !p.EsCero);
        var impagosCount = pagosValidos.Count(p => p.EsCero);
        var diasAtraso = Math.Max(0, (fechaImpresion.Date - item.FechaVencimiento.Date).Days);
        sb.Append("<tr><td colspan='2' style='vertical-align: middle; font-weight: bold; color: #333;'>");
        sb.Append("<div>Pagos: ").Append(pagosCount).Append("<br/>Impagos: ").Append(impagosCount);
        sb.Append("<br/><span style='color:#c82333;'>días atraso: ").Append(diasAtraso).Append("</span></div></td>");
        for (var i = 0; i < PagosPerRow; i++)
        {
            sb.Append("<td></td>");
        }

        sb.Append("</tr>");
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
