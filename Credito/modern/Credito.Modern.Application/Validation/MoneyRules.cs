namespace Credito.Modern.Application.Validation;

/// <summary>Montos monetarios: positivos, precisión y techos seguros.</summary>
public static class MoneyRules
{
    public const decimal MaxImporte = 99_999_999.99m;

    public static string? RequirePositive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            return $"{fieldName} debe ser mayor a cero.";
        }

        if (value > MaxImporte)
        {
            return $"{fieldName} supera el máximo permitido.";
        }

        if (TieneMasDeDosDecimales(value))
        {
            return $"{fieldName} admite como máximo 2 decimales.";
        }

        return null;
    }

    public static string? RequireNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            return $"{fieldName} no puede ser negativo.";
        }

        if (value > MaxImporte)
        {
            return $"{fieldName} supera el máximo permitido.";
        }

        if (TieneMasDeDosDecimales(value))
        {
            return $"{fieldName} admite como máximo 2 decimales.";
        }

        return null;
    }

    public static string? RequireInRange(decimal value, decimal min, decimal max, string fieldName)
    {
        if (value < min || value > max)
        {
            return $"{fieldName} debe estar entre {min} y {max}.";
        }

        if (TieneMasDeDosDecimales(value))
        {
            return $"{fieldName} admite como máximo 2 decimales.";
        }

        return null;
    }

    private static bool TieneMasDeDosDecimales(decimal valor)
    {
        var scaled = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        return scaled != valor;
    }
}
