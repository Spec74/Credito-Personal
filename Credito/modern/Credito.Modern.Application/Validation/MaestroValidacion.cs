namespace Credito.Modern.Application.Validation;

/// <summary>Catálogos maestros (marca, modelo, tipo, almacén, ocupación, etc.).</summary>
public static class MaestroValidacion
{
    public static string? ValidarDenominacion(string? denominacion, string fieldName = "denominacion") =>
        StringRules.RequireText(denominacion, fieldName, StringRules.MaxDenominacion);

    public static string? ValidarDenominacionYPadre(
        string? denominacion,
        int padreId,
        string padreFieldName)
    {
        return ValidationGate.First(
            ValidarDenominacion(denominacion),
            IdRules.RequirePositive(padreId, padreFieldName));
    }
}
