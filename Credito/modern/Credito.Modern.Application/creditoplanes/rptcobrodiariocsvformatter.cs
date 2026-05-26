using System.Globalization;
using System.Text;
using Credito.Modern.Application;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCobroDiarioReadService"/>.
/// </summary>
public static class RptCobroDiarioCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCobroDiarioRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 128));
        sb.AppendLine(
            "Nro,Orden,CreditoId,Cliente,Celular,MontoCredito,Interes,CuotaPlan,Saldo,DiasAtrazo,NroCuotasPen,CuotaTotal,Direccion,FechaPago,FechaPrimerPago,FechaVencimiento,Mora,MontoTotal,Negocio,FormaPago,TopeCredito,ClasificacionRiesgoSBS");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Nro?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Orden?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.CuotaPlan?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Saldo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.DiasAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.NroCuotasPen?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.CuotaTotal?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Direccion)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaPago, inv)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaPrimerPago, inv)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaVencimiento, inv)).Append(',')
                .Append(r.Mora?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.MontoTotal?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Negocio)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.TopeCredito?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.ClasificacionRiesgoSBS))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
