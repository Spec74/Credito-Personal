using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptSaldosCajaReadService"/>.
/// </summary>
public static class RptSaldosCajaCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptSaldosCajaRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "MovimientoCajaId,Operacion,FechaReg,Codigo,Cliente,ImportePago,IndEntrada,Glosa,TipoPago");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoCajaId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Operacion)).Append(',')
                .Append(r.FechaReg.ToString(DateTimeFormat, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Codigo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.ImportePago.ToString(inv)).Append(',')
                .Append(r.IndEntrada.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Glosa)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.TipoPago))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
