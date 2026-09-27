using System.Text.RegularExpressions;

namespace Credito.Modern.Application.Validation;

/// <summary>Reglas de texto, documentos peruanos y longitudes máximas compartidas.</summary>
public static partial class StringRules
{
    public const int MaxDenominacion = 100;
    public const int MaxNombre = 100;
    public const int MaxGlosa = 250;
    public const int MaxObservacion = 500;
    public const int MaxUsuario = 50;
    public const int MaxEmail = 120;
    public const int MaxDireccion = 250;
    public const int MaxSerie = 50;

    public static string? RequireText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"{fieldName} es obligatorio.";
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            return $"{fieldName} no puede superar {maxLength} caracteres.";
        }

        return null;
    }

    public static string? OptionalMaxLength(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Trim().Length > maxLength)
        {
            return $"{fieldName} no puede superar {maxLength} caracteres.";
        }

        return null;
    }

    public static string? RequireDni(string? value)
    {
        var digits = SoloDigitos().Replace(value ?? string.Empty, string.Empty);
        return digits.Length == 8 ? null : "El DNI debe tener 8 dígitos.";
    }

    public static string? RequireRuc(string? value)
    {
        var digits = SoloDigitos().Replace(value ?? string.Empty, string.Empty);
        return digits.Length == 11 ? null : "El RUC debe tener 11 dígitos.";
    }

    public static string? RequireDocumento(string? value, string tipoPersona)
    {
        return string.Equals(tipoPersona, "J", StringComparison.OrdinalIgnoreCase)
            ? RequireRuc(value)
            : RequireDni(value);
    }

    /// <summary>Celular móvil peruano: 9 dígitos que empiezan con 9. Vacío = válido si no es obligatorio.</summary>
    public static string? OptionalCelularPeruano(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return EsCelularPeruano(value)
            ? null
            : "El celular debe tener 9 dígitos y empezar con 9.";
    }

    public static string? RequireCelularPeruano(string? value) =>
        EsCelularPeruano(value)
            ? null
            : "El celular es obligatorio (9 dígitos que empiezan con 9).";

    public static bool EsCelularPeruano(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = SoloDigitos().Replace(value, string.Empty);
        return digits.Length == 9 && digits[0] == '9';
    }

    public static string? OptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var v = value.Trim();
        if (v.Length > MaxEmail)
        {
            return $"El correo no puede superar {MaxEmail} caracteres.";
        }

        return v.Contains('@', StringComparison.Ordinal) && v.IndexOf('.', v.IndexOf('@')) > 0
            ? null
            : "El correo no tiene un formato válido.";
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex SoloDigitos();
}
