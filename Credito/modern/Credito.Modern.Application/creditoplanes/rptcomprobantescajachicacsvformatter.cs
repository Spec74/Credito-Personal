using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptComprobantesCajaChicaCsvFormatter
{
    private const string DateFormat = "yyyy-MM-dd";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptComprobantesCajaChicaRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "Gasto,Fecha,Documento,Serie,Numero,RUC,RazonSocial,DetalleGasto,Importe");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Gasto)).Append(',')
                .Append(r.Fecha.ToString(DateFormat, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Documento)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Serie)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Numero)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Ruc)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.RazonSocial)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DetalleGasto)).Append(',')
                .Append(r.Importe.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
