using System.Globalization;
using System.Text;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Archivo TXT regulatorio DM007898 (paridad <c>ReporteController.ReporteCentrarRiegoTXT</c>).
/// </summary>
public static class CentralRiesgoGenerarTxtFormatter
{
    public static byte[] ToUtf8Bytes(IReadOnlyList<CentralRiesgoGenerarRowDto> rows)
    {
        var sb = new StringBuilder(Math.Max(256, rows.Count * 400));
        foreach (var r in rows)
        {
            sb.AppendLine(FormatLine(r));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    internal static string FormatLine(CentralRiesgoGenerarRowDto r)
    {
        var vacio = string.Empty;
        var deudaMenor = (r.DeudaMenor30 ?? string.Empty).Replace(".", string.Empty, StringComparison.Ordinal);
        var deudaMayor = (r.DeudaMayor30 ?? string.Empty).Replace(".", string.Empty, StringComparison.Ordinal);

        return (r.Periodo ?? string.Empty)
            + (r.Entidad ?? string.Empty)
            + r.TipoDoc.ToString(CultureInfo.InvariantCulture).PadLeft(31, ' ')
            + (r.NumDoc ?? string.Empty).PadLeft(12, ' ')
            + (r.RazonSocial ?? string.Empty).PadLeft(100, ' ')
            + (r.ApePat ?? string.Empty).PadRight(20, ' ')
            + (r.ApeMat ?? string.Empty).PadRight(20, ' ')
            + (r.Nombres ?? string.Empty).PadRight(30, ' ')
            + r.TipoPersona.ToString(CultureInfo.InvariantCulture)
            + r.ModalidadCredito.ToString(CultureInfo.InvariantCulture)
            + deudaMenor.PadLeft(39, ' ')
            + deudaMayor.PadLeft(13, ' ')
            + vacio.PadLeft(65, ' ')
            + vacio.PadLeft(117, ' ')
            + r.Calificacion.ToString(CultureInfo.InvariantCulture)
            + r.DiasAtrazo.ToString(CultureInfo.InvariantCulture).PadRight(5, ' ')
            + (r.Direccion ?? string.Empty).PadRight(80, ' ')
            + vacio.PadLeft(120, ' ')
            + (r.celular ?? string.Empty).PadRight(10, ' ');
    }
}
