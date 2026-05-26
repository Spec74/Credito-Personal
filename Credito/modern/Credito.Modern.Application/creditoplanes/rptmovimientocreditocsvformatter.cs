using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptMovimientoCreditoReadService"/>.
/// </summary>
public static class RptMovimientoCreditoCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptMovimientoCreditoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 64));
        sb.AppendLine("MovimientoCajaId,Fecha,Operacion,Glosa,ImportePago,Saldo");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoCajaId?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Fecha is null ? string.Empty : r.Fecha.Value.ToString(DateTimeFormat, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Operacion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Glosa)).Append(',')
                .Append(r.ImportePago?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Saldo?.ToString(inv) ?? string.Empty)
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
