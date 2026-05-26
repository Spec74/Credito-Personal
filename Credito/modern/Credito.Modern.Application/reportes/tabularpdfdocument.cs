using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// PDF desde CSV UTF-8 BOM con layout legacy (<see cref="CredixLegacyPdfDocument"/>).
/// Si el título coincide con un informe del catálogo, aplica mapeo de columnas RDLC.
/// </summary>
public static class TabularPdfDocument
{
    public static byte[] FromUtf8BomCsv(
        string reportTitle,
        byte[] csvUtf8Bom,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine>? metadata = null,
        CredixLegacyReportContext? context = null)
    {
        if (CredixLegacyReportCatalog.TryGetByLegacyTitle(reportTitle, out var key))
            return CredixLegacyPdfExports.FromCsv(key, csvUtf8Bom, context, metadata);

        var text = DecodeUtf8(csvUtf8Bom);
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var start = 0;
        if (lines.Length > 0
            && lines[0].StartsWith(CsvUtf8BomEncoding.ExcelSeparatorHintLine, StringComparison.OrdinalIgnoreCase))
        {
            start = 1;
        }

        if (lines.Length <= start)
            return Build(reportTitle, Array.Empty<string>(), Array.Empty<IReadOnlyList<string>>(), metadata);

        var headers = CsvLineParser.ParseLine(lines[start]);
        var rows = new List<IReadOnlyList<string>>(Math.Max(0, lines.Length - start - 1));
        for (var i = start + 1; i < lines.Length; i++)
            rows.Add(CsvLineParser.ParseLine(lines[i]));

        return Build(reportTitle, headers, rows, metadata);
    }

    public static byte[] FromUtf8BomCsv(
        CredixLegacyReportKey key,
        byte[] csvUtf8Bom,
        CredixLegacyReportContext? context = null,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine>? metadata = null) =>
        CredixLegacyPdfExports.FromCsv(key, csvUtf8Bom, context, metadata);

    public static byte[] Build(
        string reportTitle,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine>? metadata = null)
    {
        return CredixLegacyPdfDocument.FromTable(
            new CredixLegacyPdfDocument.ReportTable(
                reportTitle,
                metadata ?? Array.Empty<CredixLegacyPdfDocument.MetadataLine>(),
                headers,
                rows));
    }

    private static string DecodeUtf8(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes.AsSpan(3));
        return Encoding.UTF8.GetString(bytes);
    }
}
