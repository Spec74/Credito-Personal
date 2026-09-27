using System.Globalization;

namespace Credito.Modern.Application.Validation;

/// <summary>Fechas de operación: no nulas, no futuras/pasadas según regla de negocio.</summary>
public static class DateRules
{
    public static string? RequireDate(DateTime value, string fieldName) =>
        value == default ? $"{fieldName} es obligatoria." : null;

    public static string? RequireNotBefore(DateTime value, DateTime minInclusive, string fieldName)
    {
        if (value == default)
        {
            return $"{fieldName} es obligatoria.";
        }

        if (value.Date < minInclusive.Date)
        {
            return $"{fieldName} no puede ser anterior a {minInclusive.ToString("dd/MM/yyyy", CulturaPe)}.";
        }

        return null;
    }

    public static string? RequireNotAfter(DateTime value, DateTime maxInclusive, string fieldName)
    {
        if (value == default)
        {
            return $"{fieldName} es obligatoria.";
        }

        if (value.Date > maxInclusive.Date)
        {
            return $"{fieldName} no puede ser posterior a {maxInclusive.ToString("dd/MM/yyyy", CulturaPe)}.";
        }

        return null;
    }

    private static CultureInfo CulturaPe { get; } = CultureInfo.GetCultureInfo("es-PE");
}
