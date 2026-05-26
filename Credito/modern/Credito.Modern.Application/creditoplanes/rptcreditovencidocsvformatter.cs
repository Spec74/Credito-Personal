using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoVencidoReadService"/>.
/// </summary>
public static class RptCreditoVencidoCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoVencidoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "Gestor,CreditoId,Cliente,MontoCredito,FormaPago,FechaVencimiento,CreditoVencido,VencidoMenor60,VencidoMayor60,VencidoIrrecuperable");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Gestor)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.FechaVencimiento.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.CreditoVencido?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.VencidoMenor60)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.VencidoMayor60)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.VencidoIrrecuperable))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
