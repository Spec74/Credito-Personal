namespace Credito.Modern.Application.Validation;

/// <summary>
/// Agrega la primera falla de una secuencia de reglas (null = ok).
/// Uso: <c>ValidationGate.First(IdRules.RequirePositive(...), StringRules.RequireText(...))</c>
/// </summary>
public static class ValidationGate
{
    public static string? First(params string?[] errors)
    {
        foreach (var error in errors)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                return error;
            }
        }

        return null;
    }

    public static void ThrowIfInvalid(params string?[] errors)
    {
        var first = First(errors);
        if (first is not null)
        {
            throw new ArgumentException(first);
        }
    }
}
