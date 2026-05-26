using System.Globalization;
using System.Text;
using Credito.Modern.Application;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoMorosidadReadService"/>.
/// </summary>
public static class RptCreditoMorosidadCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoMorosidadRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 128));
        sb.AppendLine(
            "CreditoId,Cliente,Direccion,Celular,FechaDesembolso,FechaVcto,Articulo,MontoCredito,SaldoCredito,FechaUltPago,CapitalAtrazo,GA,InteresAtrazo,Mora,ImporteLibre,DiasAtrazo,CuotasAtrazo,DeudaAtrazo");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Direccion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaDesembolso, inv)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaVcto, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Articulo)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.SaldoCredito?.ToString(inv) ?? string.Empty).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaUltPago, inv))
                .Append(',')
                .Append(r.CapitalAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.GA?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.InteresAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Mora?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.ImporteLibre?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.DiasAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.CuotasAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.DeudaAtrazo?.ToString(inv) ?? string.Empty)
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
