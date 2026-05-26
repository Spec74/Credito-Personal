using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoRentabilidadReadService"/>.
/// </summary>
public static class RptCreditoRentabilidadCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoRentabilidadRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 120));
        sb.AppendLine(
            "CreditoId,Oficina,Codigo,Cliente,FechaDesembolso,FechaPago,NumeroCuotas,FormaPago,Estado,MontoCredito,Interes,SumCuota,CuotasPagadas,MontoGastosAdm,SumInteres,SumMora,SumPago");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Oficina)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Codigo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(
                    r.FechaDesembolso is null
                        ? string.Empty
                        : r.FechaDesembolso.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(
                    r.FechaPago is null
                        ? string.Empty
                        : r.FechaPago.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(r.NumeroCuotas.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.SumCuota?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.CuotasPagadas?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.MontoGastosAdm.ToString(inv)).Append(',')
                .Append(r.SumInteres?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SumMora?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SumPago?.ToString(inv) ?? string.Empty)
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
