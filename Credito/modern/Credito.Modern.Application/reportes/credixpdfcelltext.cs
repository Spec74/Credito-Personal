using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Tipografía de celdas PDF: tokens compactos (DNI, montos, fechas) en una sola línea
/// con fuente autoajustable — sin elipsis ni saltos a mitad de token.
/// </summary>
public static class CredixPdfCellText
{
    public const float MinFontSize = 5.2f;

    /// <summary>
    /// Texto de celda legible: reduce el tamaño de fuente si el token no cabe;
    /// nunca usa puntos suspensivos que aparenten dato incompleto.
    /// </summary>
    public static void Write(
        IContainer cell,
        string? value,
        float fontSize,
        bool bold = false,
        Color? color = null)
    {
        var text = value ?? string.Empty;
        var size = FitFontSize(text, fontSize, IsNonBreakingToken(text) ? 9 : 14);
        cell.Text(descriptor =>
        {
            var span = descriptor.Span(text).FontSize(size);
            if (bold)
            {
                span.Bold();
            }

            if (color is { } c)
            {
                span.FontColor(c);
            }
        });
    }

    /// <summary>
    /// Reduce la fuente según longitud del texto para que quepa en columnas estrechas.
    /// </summary>
    public static float FitFontSize(string? value, float baseSize, int softMaxChars = 11)
    {
        if (string.IsNullOrEmpty(value) || baseSize <= MinFontSize)
            return Math.Max(MinFontSize, baseSize);

        var len = value.Trim().Length;
        if (len <= softMaxChars)
            return baseSize;

        // Escala suave: ~11 chars a baseSize; ~22 chars a ~70 %; piso MinFontSize.
        var scale = softMaxChars / (float)len;
        var fitted = baseSize * Math.Clamp(scale * 1.25f, 0.62f, 1f);
        return Math.Clamp(fitted, MinFontSize, baseSize);
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
