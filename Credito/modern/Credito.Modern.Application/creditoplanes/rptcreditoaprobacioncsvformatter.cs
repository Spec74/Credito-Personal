using System.Globalization;
using System.Text;
using Credito.Modern.Application;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoAprobacionReadService"/>.
/// </summary>
public static class RptCreditoAprobacionCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoAprobacionRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "CreditoId,Oficina,Cliente,FechaAprobacion,MontoCredito,Interes,NumeroCuotas,MontoDesembolso,Gestor");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Oficina)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaAprobacion, inv))
                .Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.NumeroCuotas.ToString(inv)).Append(',')
                .Append(r.MontoDesembolso.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Gestor))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
