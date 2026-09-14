using System.Globalization;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Totales por columna para el pie de los informes PDF. Se declaran por informe (no se suma
/// cualquier columna numérica: porcentajes y conteos no se totalizan).
/// </summary>
public static class CredixReportTotals
{
    /// <summary>
    /// Suma las columnas indicadas por nombre de CSV. Devuelve el índice de columna y su total;
    /// las celdas no numéricas se ignoran (el informe puede traer vacíos).
    /// </summary>
    public static IReadOnlyDictionary<int, decimal> Compute(
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<string>? totalColumns)
    {
        if (columnSpecs is null || totalColumns is null || totalColumns.Count == 0 || rows.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var indexes = new Dictionary<int, decimal>();
        for (var c = 0; c < columnSpecs.Count; c++)
        {
            if (totalColumns.Contains(columnSpecs[c].CsvName, StringComparer.OrdinalIgnoreCase))
            {
                indexes[c] = 0m;
            }
        }

        if (indexes.Count == 0)
        {
            return indexes;
        }

        foreach (var row in rows)
        {
            foreach (var c in indexes.Keys.ToList())
            {
                if (c >= row.Count)
                {
                    continue;
                }

                if (decimal.TryParse(
                        row[c],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var value))
                {
                    indexes[c] += value;
                }
            }
        }

        return indexes;
    }

    public static string Format(decimal total) =>
        total.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
}
