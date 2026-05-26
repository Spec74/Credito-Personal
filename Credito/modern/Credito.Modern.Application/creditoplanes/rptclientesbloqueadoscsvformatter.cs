using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptClientesBloqueadosReadService"/>.
/// </summary>
public static class RptClientesBloqueadosCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptClientesBloqueadosRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 80));
        sb.AppendLine("Agente,NumeroDocumento,Cliente,Direccion,DireccionRef,Celular,Calificacion,Nota");
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.NumeroDocumento)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Direccion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DireccionRef)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Calificacion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Nota))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
