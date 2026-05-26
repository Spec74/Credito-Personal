using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Ventas;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptListaPrecioGeneralReadService"/>.
/// </summary>
public static class RptListaPrecioCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptListaPrecioGeneralRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 48));
        sb.AppendLine("ArticuloId,TipoArticulo,ArticuloDes,Monto,Descuento,PuntosCanje");
        foreach (var r in rows)
        {
            sb.Append(r.ArticuloId.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.TipoArticulo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.ArticuloDes)).Append(',')
                .Append(r.Monto.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(r.Descuento is null ? string.Empty : r.Descuento.Value.ToString(CultureInfo.InvariantCulture))
                .Append(',')
                .Append(r.PuntosCanje is null
                    ? string.Empty
                    : r.PuntosCanje.Value.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
