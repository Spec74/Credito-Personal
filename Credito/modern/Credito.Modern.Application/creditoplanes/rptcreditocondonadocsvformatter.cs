using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoCondonadoReadService"/>.
/// </summary>
public static class RptCreditoCondonadoCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoCondonadoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 96));
        sb.AppendLine(
            "OficinaId,Oficina,CreditoId,Cliente,FechaPrimerPago,FechaVencimiento,MontoCredito,Interes,MontoCondonado,AgenteId,Agente,Observacion");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.OficinaId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Oficina)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.FechaPrimerPago.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.FechaVencimiento.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.MontoCondonado.ToString(inv)).Append(',')
                .Append(r.AgenteId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Observacion))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
