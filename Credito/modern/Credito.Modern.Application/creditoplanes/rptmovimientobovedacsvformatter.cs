using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptMovimientoBovedaReadService"/>.
/// </summary>
public static class RptMovimientoBovedaCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptMovimientoBovedaRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "MovimientoBovedaId,FechaReg,CodOperacion,Glosa,Entrada,Salida,TipoPago,Agente");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoBovedaId.ToString(inv)).Append(',')
                .Append(r.FechaReg.ToString(DateTimeFormat, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.CodOperacion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Glosa)).Append(',')
                .Append(r.Entrada?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Salida?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.TipoPago)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
