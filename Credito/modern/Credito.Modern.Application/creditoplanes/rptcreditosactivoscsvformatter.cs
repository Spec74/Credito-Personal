using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditosActivosReadService"/>.
/// </summary>
public static class RptCreditosActivosCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditosActivosRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 160));
        sb.AppendLine(
            "Nro,Estado,Agente,CreditoId,Codigo,Cliente,MontoCredito,FormaPago,NumeroCuotas,Interes,MontoInteres,MontoCreditoTotal,MontoGastosAdm,CentralRiesgo,FechaPrimerPago,FechaVencimiento,NroCuotasPagado,Pagado,InteresPagado,NroCuotasPen,SaldoCapital,SaldoInteres,Saldo,DiasAtrazo,Mora");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Nro?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Codigo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.NumeroCuotas.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.MontoInteres?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.MontoCreditoTotal?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.MontoGastosAdm.ToString(inv)).Append(',')
                .Append(r.CentralRiesgo.ToString(inv)).Append(',')
                .Append(r.FechaPrimerPago.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.FechaVencimiento.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.NroCuotasPagado?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Pagado?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.InteresPagado.ToString(inv)).Append(',')
                .Append(r.NroCuotasPen?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SaldoCapital?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SaldoInteres?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Saldo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.DiasAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Mora?.ToString(inv) ?? string.Empty)
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
