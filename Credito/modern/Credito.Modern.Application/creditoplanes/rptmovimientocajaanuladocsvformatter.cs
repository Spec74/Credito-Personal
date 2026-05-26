using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptMovimientoCajaAnuladoReadService"/>.
/// </summary>
public static class RptMovimientoCajaAnuladoCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptMovimientoCajaAnuladoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 112));
        sb.AppendLine(
            "MovimientoCajaId,Operacion,ImportePago,Persona,Descripcion,FechaReg,UsuarioRegistro,MotivoAnulacion,FechaAnulacion,UsuarioAnulacion");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.MovimientoCajaId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Operacion)).Append(',')
                .Append(r.ImportePago.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Persona)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Descripcion)).Append(',')
                .Append(r.FechaReg.ToString(DateTimeFormat, inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.UsuarioRegistro)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.MotivoAnulacion)).Append(',')
                .Append(
                    r.FechaAnulacion is null
                        ? string.Empty
                        : r.FechaAnulacion.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.UsuarioAnulacion))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
