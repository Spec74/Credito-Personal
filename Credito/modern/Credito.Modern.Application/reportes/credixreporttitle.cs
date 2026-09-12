using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.Reportes;

/// <summary>Normaliza títulos de informe para emparejar catálogo RDLC sin depender de tildes.</summary>
public static class CredixReportTitle
{
    private static readonly Regex ExtraSpaces = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var formD = title.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                buffer.Append(ch);
            }
        }

        return ExtraSpaces.Replace(buffer.ToString().Normalize(NormalizationForm.FormC), " ");
    }
}
