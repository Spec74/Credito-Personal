using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Almacenes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IReporteStockReadService"/>.
/// </summary>
public static class ReporteStockCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<ReporteStockRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 64));
        sb.AppendLine("Nro,TipoArticulo,ArticuloId,Articulo,Stock,Series");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Nro?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.TipoArticulo)).Append(',')
                .Append(r.ArticuloId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Articulo)).Append(',')
                .Append(r.Stock.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Series))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
