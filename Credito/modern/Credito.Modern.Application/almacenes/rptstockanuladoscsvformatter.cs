using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Almacenes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptStockAnuladosReadService"/>.
/// </summary>
public static class RptStockAnuladosCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptStockAnuladoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 48));
        sb.AppendLine("MovimientoId,Movimiento,Observacion,Fecha,Cantidad,Detalle");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Movimiento)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Observacion)).Append(',')
                .Append(r.Fecha.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.Cantidad.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Detalle))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
