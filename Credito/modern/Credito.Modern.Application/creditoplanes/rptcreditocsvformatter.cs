using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptCreditoReadService"/>.
/// </summary>
public static class RptCreditoCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptCreditoRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 120));
        sb.AppendLine(
            "Producto,Cliente,CreditoId,FechaDesembolso,FechaVcto,FormaPago,NumeroCuotas,Interes,Estado,MontoProducto,MontoInicial,MontoCredito,TipoGastoAdm,MontoGastosAdm,MontoDesembolso,Observacion");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(CsvUtf8BomEncoding.EscapeField(r.Producto)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(
                    r.FechaDesembolso is null
                        ? string.Empty
                        : r.FechaDesembolso.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(
                    r.FechaVcto is null
                        ? string.Empty
                        : r.FechaVcto.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.FormaPago)).Append(',')
                .Append(r.NumeroCuotas.ToString(inv)).Append(',')
                .Append(r.Interes.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Estado)).Append(',')
                .Append(r.MontoProducto.ToString(inv)).Append(',')
                .Append(r.MontoInicial.ToString(inv)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.TipoGastoAdm)).Append(',')
                .Append(r.MontoGastosAdm.ToString(inv)).Append(',')
                .Append(r.MontoDesembolso.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Observacion))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
