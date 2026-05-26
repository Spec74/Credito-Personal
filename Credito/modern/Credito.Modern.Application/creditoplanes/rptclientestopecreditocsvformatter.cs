using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptClientesTopeCreditoReadService"/>.
/// </summary>
public static class RptClientesTopeCreditoCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptClientesTopeCreditoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 88));
        sb.AppendLine(
            "Agente,NumeroDocumento,Cliente,Direccion,DireccionRef,Celular,Calificacion,TopeCredito,Nota");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.NumeroDocumento)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Direccion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DireccionRef)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Calificacion)).Append(',')
                .Append(r.TopeCredito?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Nota))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
