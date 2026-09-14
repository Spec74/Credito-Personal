using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.Reportes;

/// <summary>PDF legacy desde CSV usando <see cref="CredixLegacyReportCatalog"/>.</summary>
public static class CredixLegacyCsvPdfDocument
{
    public static byte[] Build(
        CredixLegacyReportKey key,
        byte[] csvUtf8Bom,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine>? metadata = null,
        CredixLegacyReportContext? context = null)
    {
        var def = CredixLegacyReportCatalog.Get(key);
        var ctx = context ?? new CredixLegacyReportContext();
        var meta = metadata ?? CredixLegacyReportCatalog.BuildMetadata(key, ctx);
        var title = string.IsNullOrWhiteSpace(ctx.Titulo) ? def.Title : ctx.Titulo.Trim();
        var (headers, rows) = ParseCsv(csvUtf8Bom);
        var mapped = MapRows(def, headers, rows);
        return CredixLegacyPdfDocument.FromTable(
            new CredixLegacyPdfDocument.ReportTable(
                title,
                meta,
                mapped.Headers,
                mapped.Rows,
                def.Landscape,
                RowCountFooter: mapped.Rows.Count,
                ColumnSpecs: def.Columns,
                TotalColumns: def.TotalColumns));
    }

    private static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) MapRows(
        CredixLegacyReportDefinition def,
        IReadOnlyList<string> csvHeaders,
        IReadOnlyList<IReadOnlyList<string>> csvRows)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < csvHeaders.Count; i++)
        {
            if (!index.ContainsKey(csvHeaders[i]))
                index[csvHeaders[i]] = i;
        }

        string Cell(IReadOnlyList<string> row, string csvName)
        {
            if (!index.TryGetValue(csvName, out var i) || i >= row.Count)
                return string.Empty;
            return row[i] ?? string.Empty;
        }

        var displayHeaders = def.Columns.Select(c => c.DisplayLabel).ToList();
        var outRows = new List<IReadOnlyList<string>>(csvRows.Count);
        foreach (var row in csvRows)
        {
            var cells = new List<string>(def.Columns.Count);
            foreach (var col in def.Columns)
                cells.Add(Cell(row, col.CsvName));
            outRows.Add(cells);
        }

        return (displayHeaders, outRows);
    }

    private static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) ParseCsv(byte[] csvUtf8Bom)
    {
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
            return (Array.Empty<string>(), Array.Empty<IReadOnlyList<string>>());

        var headers = CsvLineParser.ParseLine(lines[start]);
        var rows = new List<IReadOnlyList<string>>(Math.Max(0, lines.Length - start - 1));
        for (var i = start + 1; i < lines.Length; i++)
            rows.Add(CsvLineParser.ParseLine(lines[i]));
        return (headers, rows);
    }

    private static string DecodeUtf8(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes.AsSpan(3));
        return Encoding.UTF8.GetString(bytes);
    }
}
