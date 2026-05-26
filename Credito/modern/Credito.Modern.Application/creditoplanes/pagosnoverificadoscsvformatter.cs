using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IPagosNoVerificadosReadService"/>.
/// </summary>
public static class PagosNoVerificadosCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<PagosNoVerificadosRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 64));
        sb.AppendLine(
            "MovimientoCajaId,Cliente,Movimiento,ImportePago,TipoPago,FechaTransferencia,Registro");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoCajaId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Movimiento)).Append(',')
                .Append(r.ImportePago.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.TipoPago)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FechaTransferencia)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Registro))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
