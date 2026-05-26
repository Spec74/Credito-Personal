using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptClienteCsvFormatter
{
    public static byte[] ToUtf8BomCsv(RptClienteInformeDto informe)
    {
        var sb = new StringBuilder();
        var f = informe.Ficha;
        sb.AppendLine("Campo,Valor");
        sb.AppendLine($"Creditos,{f.CreditosDesembolsados}");
        sb.AppendLine($"Cliente,{CsvUtf8BomEncoding.EscapeField(f.Cliente)}");
        sb.AppendLine($"NumeroDocumento,{CsvUtf8BomEncoding.EscapeField(f.NumeroDocumento)}");
        sb.AppendLine($"FechaNacimiento,{CsvUtf8BomEncoding.EscapeField(f.FechaNacimiento)}");
        sb.AppendLine($"Sexo,{CsvUtf8BomEncoding.EscapeField(f.Sexo)}");
        sb.AppendLine($"Direccion,{CsvUtf8BomEncoding.EscapeField(f.Direccion)}");
        sb.AppendLine($"DireccionRef,{CsvUtf8BomEncoding.EscapeField(f.DireccionRef)}");
        sb.AppendLine($"Celular,{CsvUtf8BomEncoding.EscapeField(f.Celular)}");
        sb.AppendLine($"Conyugue,{CsvUtf8BomEncoding.EscapeField(f.Conyugue)}");
        sb.AppendLine($"ConyugueDNI,{CsvUtf8BomEncoding.EscapeField(f.ConyugueDni)}");
        sb.AppendLine($"ConyugueCelular,{CsvUtf8BomEncoding.EscapeField(f.ConyugueCelular)}");
        sb.AppendLine($"TipoVivienda,{CsvUtf8BomEncoding.EscapeField(f.TipoVivienda)}");
        sb.AppendLine($"EstadoCivil,{CsvUtf8BomEncoding.EscapeField(f.EstadoCivil)}");
        sb.AppendLine($"Distrito,{CsvUtf8BomEncoding.EscapeField(f.Distrito)}");
        sb.AppendLine($"ActividadEconomica,{CsvUtf8BomEncoding.EscapeField(f.ActividadEconomica)}");
        sb.AppendLine($"Nota,{CsvUtf8BomEncoding.EscapeField(f.Nota)}");
        sb.AppendLine($"DireccionNegocio,{CsvUtf8BomEncoding.EscapeField(f.DireccionNegocio)}");
        sb.AppendLine($"DireccionNegocioref,{CsvUtf8BomEncoding.EscapeField(f.DireccionNegocioRef)}");
        sb.AppendLine();
        sb.AppendLine("Grupo,CreditoId,MontoCredito,Estado,Persona,Dni,Celular");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in informe.Avales)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Grupo)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Persona)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Dni)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
