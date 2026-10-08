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

    /// <summary>
    /// Fecha de nacimiento opcional: si viene, no futura, edad entre <paramref name="minAgeYears"/> y 120.
    /// </summary>
    public static string? OptionalFechaNacimiento(
        DateTime? value,
        DateTime today,
        int minAgeYears = 18,
        string fieldName = "fecha de nacimiento")
    {
        if (value is null)
        {
            return null;
        }

        var birth = value.Value.Date;
        var hoy = today.Date;
        if (birth > hoy)
        {
            return $"{fieldName} no puede ser una fecha futura.";
        }

        var edad = EdadEnAnios(birth, hoy);
        if (edad > 120)
        {
            return $"{fieldName} no es válida (edad mayor a 120 años).";
        }

        if (edad < minAgeYears)
        {
            return $"{fieldName}: debe tener al menos {minAgeYears} años.";
        }

        return null;
    }

    public static int EdadEnAnios(DateTime birthDate, DateTime onDate)
    {
        var age = onDate.Year - birthDate.Year;
        if (birthDate.Date > onDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static CultureInfo CulturaPe { get; } = CultureInfo.GetCultureInfo("es-PE");
}
