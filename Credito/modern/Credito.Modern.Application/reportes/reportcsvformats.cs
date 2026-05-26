using System.Globalization;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Formatos de exportación CSV/PDF alineados al legacy (fecha de negocio vs timestamp).
/// </summary>
public static class ReportCsvFormats
{
    public const string DateOnly = "yyyy-MM-dd";

    public const string DateTime = "yyyy-MM-dd HH:mm:ss";

    public static string FormatDateOnly(DateTime? value, IFormatProvider? provider = null) =>
        value is null ? string.Empty : value.Value.ToString(DateOnly, provider ?? CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime? value, IFormatProvider? provider = null) =>
        value is null ? string.Empty : value.Value.ToString(DateTime, provider ?? CultureInfo.InvariantCulture);

    /// <summary>Fecha con hora solo si no es medianoche (evita ruido en desembolsos a las 00:00).</summary>
    public static string FormatDateOrDateTime(DateTime? value, IFormatProvider? provider = null)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var v = value.Value;
        return v.TimeOfDay == TimeSpan.Zero
            ? v.ToString(DateOnly, provider ?? CultureInfo.InvariantCulture)
            : v.ToString(DateTime, provider ?? CultureInfo.InvariantCulture);
    }
}
