using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCajaDiarioReadService"/>.
/// </summary>
public static class RptCajaDiarioCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCajaDiarioRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "CajaDiarioId,Oficina,Caja,Agente,SaldoInicial,Entradas,Salidas,SaldoFinal,FechaIniOperacion,FechaFinOperacion");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.CajaDiarioId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Oficina)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Caja)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(r.SaldoInicial.ToString(inv)).Append(',')
                .Append(r.Entradas.ToString(inv)).Append(',')
                .Append(r.Salidas.ToString(inv)).Append(',')
                .Append(r.SaldoFinal.ToString(inv)).Append(',')
                .Append(r.FechaIniOperacion.ToString(DateTimeFormat, inv)).Append(',')
                .Append(
                    r.FechaFinOperacion is null
                        ? string.Empty
                        : r.FechaFinOperacion.Value.ToString(DateTimeFormat, inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
