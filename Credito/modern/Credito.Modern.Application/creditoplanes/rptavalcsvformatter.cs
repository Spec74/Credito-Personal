using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptAvalReadService"/>.
/// </summary>
public static class RptAvalCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptAvalRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 72));
        sb.AppendLine("Grupo,CreditoId,MontoCredito,Estado,Persona,Dni,Celular");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Grupo)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Persona)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Dni)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
