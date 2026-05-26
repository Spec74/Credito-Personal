using System.Globalization;
using System.Text;
using Credito.Modern.Application;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoObservadoReadService"/>.
/// </summary>
public static class RptCreditoObservadoCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoObservadoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 96));
        sb.AppendLine(
            "OficinaId,Oficina,CreditoId,Cliente,FechaPrimerPago,FechaVencimiento,MontoCredito,Interes,AgenteId,Agente,Observacion,TramiteAdm,CentralRiesgo");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.OficinaId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Oficina)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaPrimerPago, inv)).Append(',')
                .Append(ReportCsvFormats.FormatDateOrDateTime(r.FechaVencimiento, inv)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.AgenteId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Observacion)).Append(',')
                .Append(r.TramiteAdm.ToString(inv)).Append(',')
                .Append(r.CentralRiesgo.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
