using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Tipografía de celdas PDF: DNI/IDs/montos en una sola línea (sin salto a mitad de token).
/// </summary>
public static class CredixPdfCellText
{
    /// <summary>
    /// Texto de celda: tokens compactos (DNI, código, fecha, monto) en una sola línea.
    /// Si no caben, elipsis — nunca una segunda línea con fragmentos ("44").
    /// </summary>
    public static void Write(
        IContainer cell,
        string? value,
        float fontSize,
        bool bold = false,
        Color? color = null)
    {
        var text = value ?? string.Empty;
        cell.Text(descriptor =>
        {
            var span = descriptor.Span(text).FontSize(fontSize);
            if (bold)
            {
                span.Bold();
            }

            if (color is { } c)
            {
                span.FontColor(c);
            }

            if (IsNonBreakingToken(text))
            {
                descriptor.ClampLines(1, "…");
            }
        });
    }

    public static bool IsDocumentColumn(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var n = name.Trim();
        return n.Equals("Dni", StringComparison.OrdinalIgnoreCase)
            || n.Equals("DNI", StringComparison.OrdinalIgnoreCase)
            || n.Equals("RUC", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Documento", StringComparison.OrdinalIgnoreCase)
            || n.Equals("NumeroDocumento", StringComparison.OrdinalIgnoreCase)
            || n.Equals("NroDocumento", StringComparison.OrdinalIgnoreCase)
            || n.Equals("N° documento", StringComparison.OrdinalIgnoreCase)
            || n.Equals("N° Documento", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Documento", StringComparison.OrdinalIgnoreCase)
            || n.Contains("DNI", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Valor corto sin espacios: no debe partirse en dos líneas.</summary>
    public static bool IsNonBreakingToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var v = value.Trim();
        if (v.Length is < 1 or > 28)
            return false;

        if (v.Contains(' ') || v.Contains('\n') || v.Contains('\t'))
            return false;

        return true;
    }
}
