using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditosMorososPagadosReadService"/>.
/// </summary>
public static class RptCreditosMorososPagadosCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditosMorososPagadosRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "CreditoId,Cliente,MontoCredito,Interes,FormaPago,NumeroCuotas,MontoGastosAdm,CentralRiesgo,FechaPrimerPago,FechaVencimiento,FechaPagado,Agente");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.NumeroCuotas.ToString(inv)).Append(',')
                .Append(r.MontoGastosAdm.ToString(inv)).Append(',')
                .Append(r.CentralRiesgo.ToString(inv)).Append(',')
                .Append(r.FechaPrimerPago.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.FechaVencimiento.ToString(DateTimeFormat, inv)).Append(',')
                .Append(
                    r.FechaPagado is null
                        ? string.Empty
                        : r.FechaPagado.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
