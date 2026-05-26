using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptPlanPagosCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptPlanPagosRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 64));
        sb.AppendLine("Numero,Capital,FechaPago,Amortizacion,Interes,GastosAdm,Cuota");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Numero).Append(',')
                .Append(r.Capital.ToString(inv)).Append(',')
                .Append(r.FechaPago.ToString("yyyy-MM-dd", inv)).Append(',')
                .Append(r.Amortizacion.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.GastosAdm.ToString(inv)).Append(',')
                .Append(r.Cuota.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
