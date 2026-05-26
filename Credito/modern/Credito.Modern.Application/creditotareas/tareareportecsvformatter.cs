using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoTareas;

public static class TareaReporteCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<TareaReporteRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 128));
        sb.AppendLine("Nro,TareaId,CreditoId,Cliente,Analista,Subtareas,DetalleSubtareas,Estado");
        var inv = CultureInfo.InvariantCulture;
        var ordenadas = rows
            .OrderBy(r => r.ClienteNombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.ClienteDni, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.CreditoId)
            .ThenBy(r => r.TareaId)
            .ToList();
        var nro = 1;
        foreach (var r in ordenadas)
        {
            sb.Append(nro++.ToString(inv)).Append(',')
                .Append(r.TareaId.ToString(inv)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Analista)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.SubtareasResumen)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DetalleSubtareas)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
