using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Ventas;

public static class CodigoBarrasLstCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<CodigoBarrasLstRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(128, rows.Count * 80));
        sb.AppendLine("Serie1,Articulo1,Precio1,Serie2,Articulo2,Precio2");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Serie1)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Articulo1)).Append(',')
                .Append(r.Precio1.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Serie2)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Articulo2)).Append(',')
                .Append(r.Precio2.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
