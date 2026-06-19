using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptSimuladorPlanPagosCsvFormatter
{
    public static byte[] ToUtf8BomCsv(RptSimuladorPlanPagosInformeDto informe)
    {
        var sb = new StringBuilder();
        var c = informe.Cabecera;
        sb.AppendLine("Campo,Valor");
        sb.AppendLine($"Monto,{CsvUtf8BomEncoding.EscapeField(c.Monto)}");
        sb.AppendLine($"Cuotas,{CsvUtf8BomEncoding.EscapeField(c.Cuotas)}");
        sb.AppendLine($"Producto,{CsvUtf8BomEncoding.EscapeField(c.Producto)}");
        sb.AppendLine($"Fecha,{CsvUtf8BomEncoding.EscapeField(c.Fecha)}");
        sb.AppendLine($"Modalidad,{CsvUtf8BomEncoding.EscapeField(c.Modalidad)}");
        sb.AppendLine($"Cliente,{CsvUtf8BomEncoding.EscapeField(c.Cliente)}");
        sb.AppendLine($"TEM,{CsvUtf8BomEncoding.EscapeField(c.Tem)}");
        sb.AppendLine($"Desembolso,{CsvUtf8BomEncoding.EscapeField(c.Desembolso)}");
        sb.AppendLine($"GastosAdm,{CsvUtf8BomEncoding.EscapeField(c.GastosAdm)}");
        sb.AppendLine($"TipoDocumento,{CsvUtf8BomEncoding.EscapeField(c.TipoDocumento)}");
        sb.AppendLine($"NroDocumento,{CsvUtf8BomEncoding.EscapeField(c.NroDocumento)}");
        sb.AppendLine($"DireccionCliente,{CsvUtf8BomEncoding.EscapeField(c.DireccionCliente)}");
        sb.AppendLine($"DireccionNegocio,{CsvUtf8BomEncoding.EscapeField(c.DireccionNegocio)}");
        sb.AppendLine($"Prenda,{CsvUtf8BomEncoding.EscapeField(c.PrendaDescripcion)}");
        sb.AppendLine($"Asesor,{CsvUtf8BomEncoding.EscapeField(c.Asesor)}");
        sb.AppendLine($"TelefonoCliente,{CsvUtf8BomEncoding.EscapeField(c.TelefonoCliente)}");
        sb.AppendLine($"InteresesTotales,{CsvUtf8BomEncoding.EscapeField(c.InteresesTotales)}");
        sb.AppendLine($"TotalDevolver,{CsvUtf8BomEncoding.EscapeField(c.TotalDevolver)}");
        sb.AppendLine($"CuotaReferencial,{CsvUtf8BomEncoding.EscapeField(c.CuotaReferencial)}");
        sb.AppendLine($"FechaUltimoPago,{CsvUtf8BomEncoding.EscapeField(c.FechaUltimoPago)}");
        sb.AppendLine();
        sb.AppendLine("Numero,Capital,FechaPago,Amortizacion,Interes,GastosAdm,Cuota,Saldo");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in informe.Cuotas)
        {
            sb.Append(r.Numero?.ToString(inv) ?? string.Empty).Append(',')
                .Append((r.Capital ?? 0).ToString(inv)).Append(',')
                .Append(r.FechaPago?.ToString("yyyy-MM-dd", inv) ?? string.Empty).Append(',')
                .Append((r.Amortizacion ?? 0).ToString(inv)).Append(',')
                .Append((r.Interes ?? 0).ToString(inv)).Append(',')
                .Append((r.GastosAdm ?? 0).ToString(inv)).Append(',')
                .Append((r.Cuota ?? 0).ToString(inv)).Append(',')
                .Append((r.Saldo ?? 0).ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
