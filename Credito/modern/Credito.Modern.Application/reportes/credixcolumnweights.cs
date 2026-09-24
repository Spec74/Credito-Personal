namespace Credito.Modern.Application.Reportes;

/// <summary>Pesos relativos de columna para evitar reparto homogéneo en tablas anchas.</summary>
public static class CredixColumnWeights
{
    public static float For(string csvOrHeaderName, CredixColumnAlign align)
    {
        var name = csvOrHeaderName.Trim();
        if (name.Length == 0)
            return 1f;

        if (IsWideText(name))
            return 2.8f;

        if (IsMediumText(name))
            return 1.45f;

        if (align == CredixColumnAlign.Right)
            return 0.68f;

        if (name.Contains("Fecha", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Celular", StringComparison.OrdinalIgnoreCase))
            return 0.9f;

        if (IsCompactId(name))
            return 0.55f;

        return 1.05f;
    }

    private static bool IsWideText(string name) =>
        name.Contains("Cliente", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Dirección", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Direccion", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Cliente", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Direccion", StringComparison.OrdinalIgnoreCase)
        || name.Equals("DireccionRef", StringComparison.OrdinalIgnoreCase)
        || name.Equals("DireccionNegocio", StringComparison.OrdinalIgnoreCase)
        || name.Equals("DireccionNegocioRef", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Observacion", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Articulo", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ArticuloDes", StringComparison.OrdinalIgnoreCase)
        || name.Equals("RazonSocial", StringComparison.OrdinalIgnoreCase)
        || name.Equals("DetalleGasto", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Descripcion", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Persona", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Glosa", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Concepto", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Resumen", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Direccion", StringComparison.OrdinalIgnoreCase);

    private static bool IsMediumText(string name) =>
        name.Equals("Negocio", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Producto", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Agente", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Gestor", StringComparison.OrdinalIgnoreCase)
        || name.Equals("CentralRiesgo", StringComparison.OrdinalIgnoreCase)
        || name.Equals("ClasificacionRiesgoSBS", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Oficina", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Caja", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Operacion", StringComparison.OrdinalIgnoreCase)
        || name.Equals("CodOperacion", StringComparison.OrdinalIgnoreCase)
        || name.Equals("TipoPago", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Estado", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Modalidad", StringComparison.OrdinalIgnoreCase);

    private static bool IsCompactId(string name) =>
        name.Equals("Nro", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Orden", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Firma", StringComparison.OrdinalIgnoreCase)
        || name.Equals("MontoRecibido", StringComparison.OrdinalIgnoreCase)
        || name.Equals("FormaPago", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Interes", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Cred", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Codigo", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Serie", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Numero", StringComparison.OrdinalIgnoreCase);
}
