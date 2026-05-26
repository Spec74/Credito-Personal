using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditosCierresReadService"/>.
/// </summary>
public static class RptCreditosCierresCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditosCierresRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 112));
        sb.AppendLine(
            "CreditoId,Estado,Agente,Codigo,Cliente,MontoCredito,FormaPago,NumeroCuotas,Interes,MontoGastosAdm,CentralRiesgo,FechaPrimerPago,FechaVencimiento,SumAmortizacion,SumInteres,SumCuota,SumNroCuota");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Codigo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.NumeroCuotas.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.MontoGastosAdm.ToString(inv)).Append(',')
                .Append(r.CentralRiesgo.ToString(inv)).Append(',')
                .Append(r.FechaPrimerPago.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.FechaVencimiento.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.SumAmortizacion?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SumInteres?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SumCuota?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SumNroCuota?.ToString(inv) ?? string.Empty)
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
