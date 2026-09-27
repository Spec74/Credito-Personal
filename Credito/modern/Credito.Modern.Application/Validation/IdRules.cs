namespace Credito.Modern.Application.Validation;

/// <summary>Identificadores positivos (PK / FK) en contratos de escritura.</summary>
public static class IdRules
{
    public static string? RequirePositive(int value, string fieldName) =>
        value < 1 ? $"{fieldName} debe ser >= 1." : null;

    public static string? RequirePositive(long value, string fieldName) =>
        value < 1 ? $"{fieldName} debe ser >= 1." : null;

    public static string? RequirePositive(int? value, string fieldName) =>
        value is null or < 1 ? $"{fieldName} debe ser >= 1." : null;

    public static string? RequireAllPositive(IEnumerable<int>? values, string fieldName)
    {
        if (values is null)
        {
            return $"{fieldName} es obligatorio.";
        }

        var list = values as IList<int> ?? values.ToList();
        if (list.Count == 0)
        {
            return $"{fieldName} debe contener al menos un elemento.";
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] < 1)
            {
                return $"{fieldName}[{i}] debe ser >= 1.";
            }
        }

        return null;
    }
}
