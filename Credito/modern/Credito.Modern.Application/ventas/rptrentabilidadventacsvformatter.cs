using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Ventas;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptRentabilidadVentaReadService"/>.
/// </summary>
public static class RptRentabilidadVentaCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptRentabilidadVentaRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 64));
        sb.AppendLine(
            "Nro,Codigo,Articulo,MovimientoId,FechaEnt,PrecioEnt,OrdenVentaId,FechaSal,PrecioSal,Modalidad,Rentabilidad,Cliente");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Nro?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Codigo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Articulo)).Append(',')
                .Append(r.MovimientoId?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.FechaEnt is null ? string.Empty : r.FechaEnt.Value.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.PrecioEnt?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.OrdenVentaId?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.FechaSal is null ? string.Empty : r.FechaSal.Value.ToString(DateTimeFormat, inv)).Append(',')
                .Append(r.PrecioSal?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Modalidad)).Append(',')
                .Append(r.Rentabilidad?.ToString(inv) ?? string.Empty).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
