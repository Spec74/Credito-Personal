using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Almacenes;

public static class RptConstanciaAlmacenCsvFormatter
{
    public static byte[] ToUtf8BomCsv(RptConstanciaAlmacenDto informe)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        var c = informe.Cabecera;
        sb.AppendLine("Campo,Valor");
        sb.AppendLine($"MovimientoId,{c.MovimientoId}");
        sb.AppendLine($"Oficina,{CsvUtf8BomEncoding.EscapeField(c.Oficina)}");
        sb.AppendLine($"Almacen,{CsvUtf8BomEncoding.EscapeField(c.Almacen)}");
        sb.AppendLine($"Tipo,{CsvUtf8BomEncoding.EscapeField(c.Tipo)}");
        sb.AppendLine($"TipoMovimiento,{CsvUtf8BomEncoding.EscapeField(c.TipoMovimiento)}");
        sb.AppendLine($"TipoMovimientoDesc,{CsvUtf8BomEncoding.EscapeField(c.TipoMovimientoDesc)}");
        sb.AppendLine($"Fecha,{c.Fecha:yyyy-MM-dd}");
        sb.AppendLine($"Documento,{CsvUtf8BomEncoding.EscapeField(c.Documento)}");
        sb.AppendLine($"Estado,{CsvUtf8BomEncoding.EscapeField(c.Estado)}");
        sb.AppendLine($"Observacion,{CsvUtf8BomEncoding.EscapeField(c.Observacion)}");
        sb.AppendLine($"Importe,{c.Importe.ToString(inv)}");
        sb.AppendLine();
        sb.AppendLine("Cantidad,Descripcion,PrecioUnitario,Descuento,Importe");
        foreach (var d in informe.Detalle)
        {
            sb.Append(d.Cantidad).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(d.Descripcion)).Append(',')
                .Append(d.PrecioUnitario.ToString(inv)).Append(',')
                .Append(d.Descuento.ToString(inv)).Append(',')
                .Append(d.Importe.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
