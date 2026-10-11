namespace Credito.Modern.Application.Validation;

/// <summary>Reglas GPS compartidas: <c>POST …/ubicacion</c> y <c>POST …/guardar</c>.</summary>
public static class ClienteGpsRules
{
    /// <summary>
    /// Valida un par opcional (guardar). Ambos null = OK.
    /// Un solo valor, (0,0), asimétricos o fuera de Perú → error.
    /// </summary>
    public static string? ValidarOpcional(decimal? latitud, decimal? longitud)
    {
        if (latitud is null && longitud is null)
        {
            return null;
        }

        if (latitud is null || longitud is null)
        {
            return "Debe enviar latitud y longitud juntas.";
        }

        return ValidarPar(latitud.Value, longitud.Value);
    }

    /// <summary>Valida un par obligatorio (endpoint ubicación).</summary>
    public static string? ValidarPar(decimal latitud, decimal longitud)
    {
        if (latitud == 0 || longitud == 0)
        {
            return "La ubicación GPS no es válida.";
        }

        // Perú continental + margen.
        if (latitud is < -19.5m or > 0.5m || longitud is < -82.5m or > -67.5m)
        {
            return "Las coordenadas GPS están fuera del rango válido para Perú.";
        }

        return null;
    }
}
