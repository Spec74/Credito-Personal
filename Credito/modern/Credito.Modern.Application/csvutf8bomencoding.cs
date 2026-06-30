using System.Globalization;
using System.Text;

namespace Credito.Modern.Application;

/// <summary>CSV en UTF-8 con BOM (Excel); escape de campos tipo RFC 4180 y neutralización de fórmulas.</summary>
public static class CsvUtf8BomEncoding
{
    /// <summary>Primera línea para Excel con separador regional distinto de coma (p. ej. es-PE).</summary>
    public const string ExcelSeparatorHintLine = "sep=,";

    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static byte[] GetBytes(string csvBody)
    {
        if (!csvBody.StartsWith(ExcelSeparatorHintLine, StringComparison.OrdinalIgnoreCase))
        {
            csvBody = ExcelSeparatorHintLine + "\r\n" + csvBody;
        }

        var body = Utf8NoBom.GetBytes(csvBody);
        var preamble = Utf8WithBom.GetPreamble();
        if (preamble.Length == 0)
        {
            return body;
        }

        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result.AsSpan(0, preamble.Length));
        body.CopyTo(result.AsSpan(preamble.Length));
        return result;
    }

    public static string EscapeField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var safeValue = NeutralizeFormula(value);
        var needsQuotes = safeValue.AsSpan().IndexOfAny([',', '"', '\r', '\n']) >= 0;
        var escaped = safeValue.Replace("\"", "\"\"", StringComparison.Ordinal);
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private static string NeutralizeFormula(string value)
    {
        var first = FirstNonWhitespace(value);
        if (first is '=' or '+' or '-' or '@')
        {
            return "'" + value;
        }

        return value;
    }

    private static char? FirstNonWhitespace(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsWhiteSpace(c))
            {
                return c;
            }
        }

        return null;
    }
}
