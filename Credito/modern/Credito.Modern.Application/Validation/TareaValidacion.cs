namespace Credito.Modern.Application.Validation;

/// <summary>Tareas de crédito (subtareas / seguimiento).</summary>
public static class TareaValidacion
{
    public const int MaxSubtarea = 250;

    public static string? ValidarGuardar(int creditoId, IEnumerable<string?>? titulosSubtarea)
    {
        var error = IdRules.RequirePositive(creditoId, "creditoId");
        if (error is not null)
        {
            return error;
        }

        var list = (titulosSubtarea ?? Array.Empty<string?>())
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => t.Length > 0)
            .ToList();

        if (list.Count == 0)
        {
            return "Registre al menos una subtarea con descripción.";
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Length > MaxSubtarea)
            {
                return $"Subtarea #{i + 1}: no puede superar {MaxSubtarea} caracteres.";
            }
        }

        return null;
    }
}
