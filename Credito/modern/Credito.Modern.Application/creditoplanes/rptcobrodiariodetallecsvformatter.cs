using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCobroDiarioDetalleReadService"/>.
/// </summary>
public static class RptCobroDiarioDetalleCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCobroDiarioDetalleRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "Nro,Cliente,FormaPago,MontoCredito,Interes,MontoTotal,FechaPrimerPago,FechaVencimiento,Saldo,TotalPago,DiasAtrazoMora,Pagos");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Nro?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.MontoTotal?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.FechaPrimerPago.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.FechaVencimiento.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.Saldo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.TotalPago?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.DiasAtrazoMora?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Pagos))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
