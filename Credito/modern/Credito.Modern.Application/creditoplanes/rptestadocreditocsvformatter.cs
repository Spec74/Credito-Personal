using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptEstadoCreditoCsvFormatter
{
    public static byte[] ToUtf8BomCsv(RptEstadoCreditoInformeDto informe)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        var c = informe.Cabecera;
        sb.AppendLine($"CreditoId,{c.CreditoId}");
        sb.AppendLine($"Producto,{CsvUtf8BomEncoding.EscapeField(c.Producto)}");
        sb.AppendLine($"Cliente,{CsvUtf8BomEncoding.EscapeField(c.Cliente)}");
        sb.AppendLine($"Total,{c.Total.ToString(inv)}");
        sb.AppendLine();
        sb.AppendLine(
            "Numero,Capital,FechaVencimiento,Amortizacion,Interes,GastosAdm,Cuota,Estado,DiasAtrazo,ImporteMora,Descuento,Cargo,PagoLibre,FechaPagoCuota,PagoCuota");
        foreach (var r in informe.Cuotas)
        {
            sb.Append(r.Numero).Append(',')
                .Append(r.Capital.ToString(inv)).Append(',')
                .Append(r.FechaVencimiento.ToString("yyyy-MM-dd", inv)).Append(',')
                .Append(r.Amortizacion.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(r.GastosAdm.ToString(inv)).Append(',')
                .Append(r.Cuota.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(r.DiasAtrazo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.ImporteMora?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Descuento?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Cargo?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.PagoLibre.ToString(inv)).Append(',')
                .Append(r.FechaPagoCuota?.ToString("yyyy-MM-dd", inv) ?? string.Empty).Append(',')
                .Append(r.PagoCuota?.ToString(inv) ?? string.Empty)
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
