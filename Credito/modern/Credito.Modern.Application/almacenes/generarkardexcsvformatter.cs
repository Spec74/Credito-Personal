using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Almacenes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IGenerarKardexReadService"/>.
/// </summary>
public static class GenerarKardexCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<GenerarKardexRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "MovimientoDetId,Fecha,Concepto,CantEnt,PUEnt,TotalEnt,CantSal,PUSal,TotalSal,CantSaldo,PUSaldo,TotalSaldo");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoDetId?.ToString(inv) ?? string.Empty).Append(',')
                .Append(FormatFecha(r.Fecha, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Concepto)).Append(',')
                .Append(r.CantEnt?.ToString(inv) ?? string.Empty).Append(',')
                .Append(FormatDecimal(r.PUEnt, inv)).Append(',')
                .Append(FormatDecimal(r.TotalEnt, inv)).Append(',')
                .Append(r.CantSal?.ToString(inv) ?? string.Empty).Append(',')
                .Append(FormatDecimal(r.PUSal, inv)).Append(',')
                .Append(FormatDecimal(r.TotalSal, inv)).Append(',')
                .Append(r.CantSaldo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(FormatDecimal(r.PUSaldo, inv)).Append(',')
                .Append(FormatDecimal(r.TotalSaldo, inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }

    private static string FormatFecha(DateTime? fecha, IFormatProvider inv) =>
        fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd HH:mm:ss", inv) : string.Empty;

    private static string FormatDecimal(decimal? value, IFormatProvider inv) =>
        value.HasValue ? value.Value.ToString(inv) : string.Empty;
}
