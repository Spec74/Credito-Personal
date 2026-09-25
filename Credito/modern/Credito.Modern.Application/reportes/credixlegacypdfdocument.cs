using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Layout PDF alineado a informes RDLC legacy: logo, metadatos, tabla con encabezado
/// corporativo, totales y pie estándar (<see cref="CredixReportTokens"/>).
/// </summary>
public static class CredixLegacyPdfDocument
{
    public const float HeaderBorderPt = 1.25f;
    public const float BodyBorderPt = 0.5f;
    public const float FontSizeBody = CredixReportTokens.FontSizeBody;
    public const float FontSizeTitle = CredixReportTokens.FontSizeTitle;

    private static readonly Color HeaderBg = CredixReportTokens.TableHeader;
    private static readonly Color BorderColor = CredixReportTokens.Border;

    static CredixLegacyPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public sealed record MetadataLine(string Label, string Value);

    public sealed record ReportTable(
        string Title,
        IReadOnlyList<MetadataLine> Metadata,
        IReadOnlyList<string> Headers,
        IReadOnlyList<IReadOnlyList<string>> Rows,
        bool Landscape = false,
        int? RowCountFooter = null,
        IReadOnlyList<CredixLegacyColumnSpec>? ColumnSpecs = null,
        /// <summary>Columnas (nombre CSV) a totalizar en la última fila.</summary>
        IReadOnlyList<string>? TotalColumns = null);

    public static byte[] FromTable(ReportTable report)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = CredixReportTokens.NowPrinted();
        var inv = CredixReportTokens.Inv;
        var rowCount = report.RowCountFooter ?? report.Rows.Count;
        var colCount = Math.Max(1, report.Headers.Count);
        var useLandscape = EffectiveLandscape(colCount, report.Landscape);
        var (pageWidth, pageHeight) = ResolvePageSize(colCount, useLandscape);
        var bodyFont = ResolveBodyFont(colCount);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(new PageSize(pageWidth, pageHeight));
                page.Margin(colCount >= 16 ? 12 : 18);
                page.DefaultTextStyle(s => s.FontSize(bodyFont));

                var metadata = MergeMetadata(report.Metadata, report.Headers, report.Rows);
                page.Header().Column(col =>
                {
                    col.Item().Element(c => ComposeTitleBand(c, logo, report.Title, printedAt));
                    if (metadata.Count > 0)
                    {
                        col.Item().PaddingTop(4)
                            .Element(c => ComposeMetadataBlock(c, metadata));
                    }
                });

                page.Content().PaddingTop(6).Element(c =>
                    ComposeDataTable(
                        c,
                        report.Headers,
                        report.Rows,
                        report.ColumnSpecs,
                        bodyFont,
                        report.TotalColumns));

                page.Footer().Element(c => CredixReportTokens.ComposeStandardFooter(c, rowCount));
            });
        }).GeneratePdf();
    }

    public static void ComposeTitleBand(
        IContainer container,
        byte[] logo,
        string title,
        string printedAt)
    {
        container
            .BorderBottom(1)
            .BorderColor(HeaderBg)
            .PaddingBottom(6)
            .Row(row =>
            {
                row.ConstantItem(88).Height(46).Image(logo).FitArea();
                row.RelativeItem().AlignMiddle().AlignCenter().Column(col =>
                {
                    col.Item().Text(CredixReportTokens.CompanyLegalName)
                        .Bold()
                        .FontSize(8)
                        .FontColor(CredixReportTokens.Brand);
                    col.Item().PaddingTop(1).Text(title)
                        .Bold()
                        .FontSize(FontSizeTitle)
                        .FontColor(Colors.Black);
                });
                row.ConstantItem(120).AlignMiddle().AlignRight().Text(printedAt).FontSize(8);
            });
    }

    public static void ComposeMetadataBlock(
        IContainer container,
        IReadOnlyList<MetadataLine> metadata)
    {
        var items = metadata
            .Where(m => !string.IsNullOrWhiteSpace(m.Value))
            .ToList();
        if (items.Count == 0)
            return;

        container
            .Background(CredixReportTokens.MetaBg)
            .Border(0.6f)
            .BorderColor(BorderColor)
            .PaddingVertical(4)
            .PaddingHorizontal(6)
            .Column(col =>
            {
                for (var i = 0; i < items.Count; i += 3)
                {
                    var slice = items.Skip(i).Take(3).ToList();
                    col.Item().PaddingTop(i == 0 ? 0 : 2).Row(row =>
                    {
                        foreach (var line in slice)
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span(FormatMetadataLabel(line.Label)).Bold();
                                t.Span(line.Value.Trim());
                            });
                        }

                        for (var pad = slice.Count; pad < 3; pad++)
                            row.RelativeItem();
                    });
                }
            });
    }

    /// <summary>
    /// Completa el encabezado con Oficina/Agente/Caja únicos de la tabla
    /// cuando el contexto del filtro no los trajo.
    /// </summary>
    public static IReadOnlyList<MetadataLine> MergeMetadata(
        IReadOnlyList<MetadataLine> existing,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var lines = existing
            .Where(m => !string.IsNullOrWhiteSpace(m.Value))
            .ToList();

        TryAddDistinctColumn(lines, headers, rows, "Oficina: ", "Oficina");
        TryAddDistinctColumn(lines, headers, rows, "Agente: ", "Agente", "Gestor");
        TryAddDistinctColumn(lines, headers, rows, "Caja: ", "Caja");
        return lines;
    }

    public static void ComposeDataTable(
        IContainer container,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs = null,
        float fontSize = FontSizeBody,
        IReadOnlyList<string>? totalColumns = null)
    {
        var colCount = Math.Max(1, headers.Count);
        var weights = ComputeContentWeights(headers, rows, columnSpecs);
        var totals = CredixReportTotals.Compute(columnSpecs, rows, totalColumns);
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (var c = 0; c < colCount; c++)
                    columns.RelativeColumn(weights[c]);
            });

            table.Header(header =>
            {
                for (var c = 0; c < colCount; c++)
                {
                    var label = c < headers.Count ? headers[c] : string.Empty;
                    var align = ColumnAlign(c, headers, columnSpecs);
                    CredixPdfCellText.Write(
                        header.Cell().Element(HeaderCellFor(align)),
                        label,
                        fontSize,
                        bold: true,
                        color: Colors.White);
                }
            });

            foreach (var row in rows)
            {
                for (var c = 0; c < colCount; c++)
                {
                    var raw = c < row.Count ? row[c] : string.Empty;
                    var spec = columnSpecs != null && c < columnSpecs.Count ? columnSpecs[c] : null;
                    var align = ColumnAlign(c, headers, columnSpecs);
                    CredixPdfCellText.Write(
                        table.Cell().Element(BodyCellFor(align)),
                        FormatDisplayCell(raw, spec),
                        fontSize);
                }
            }

            if (totals.Count > 0)
            {
                ComposeTotalsRow(table, colCount, headers, columnSpecs, totals, fontSize);
            }
        });
    }

    /// <summary>Fila «TOTAL» al cierre de la tabla, con el mismo realce que la cabecera.</summary>
    private static void ComposeTotalsRow(
        TableDescriptor table,
        int colCount,
        IReadOnlyList<string> headers,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs,
        IReadOnlyDictionary<int, decimal> totals,
        float fontSize)
    {
        var firstTotal = totals.Keys.Min();
        if (firstTotal > 0)
        {
            table.Cell()
                .ColumnSpan((uint)firstTotal)
                .Element(TotalsCell)
                .AlignRight()
                .Element(c => CredixPdfCellText.Write(c, "TOTAL", fontSize, bold: true));
        }

        for (var c = firstTotal; c < colCount; c++)
        {
            var cell = table.Cell().Element(TotalsCell);
            if (totals.TryGetValue(c, out var total))
            {
                CredixPdfCellText.Write(
                    cell.AlignRight(),
                    CredixReportTotals.Format(total),
                    fontSize,
                    bold: true);
                continue;
            }

            // Columna sin total: se mantiene la celda para no descuadrar la fila.
            CredixPdfCellText.Write(cell, string.Empty, fontSize);
        }
    }

    private static IContainer TotalsCell(IContainer container) =>
        container
            .Background(CredixReportTokens.TotalsBg)
            .Border(HeaderBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle();

    /// <summary>
    /// Mínimo de columnas para justificar A4/A3 horizontal.
    /// A partir de 8 columnas el vertical obliga a filas altas o texto cortado.
    /// </summary>
    public const int MinColumnsForLandscape = 8;

    /// <summary>
    /// Horizontal si hay ≥ <see cref="MinColumnsForLandscape"/> columnas,
    /// o si el informe lo pide explícitamente (p. ej. fichas anchas con menos columnas).
    /// </summary>
    public static bool EffectiveLandscape(int columnCount, bool preferLandscape) =>
        preferLandscape || columnCount >= MinColumnsForLandscape;

    /// <summary>
    /// A4 vertical por defecto; A3 o hoja extra-ancha solo en horizontal con muchas columnas.
    /// </summary>
    public static (float Width, float Height) ResolvePageSize(int columnCount, bool landscape)
    {
        if (columnCount >= 20)
            return landscape ? (1480f, 842f) : (842f, 1480f);
        if (columnCount >= 12)
        {
            var a3 = landscape ? PageSizes.A3.Landscape() : PageSizes.A3;
            return (a3.Width, a3.Height);
        }

        var a4 = landscape ? PageSizes.A4.Landscape() : PageSizes.A4;
        return (a4.Width, a4.Height);
    }

    public static float ResolveBodyFont(int columnCount) =>
        columnCount >= 20 ? 6.2f : columnCount >= 12 ? 6.8f : FontSizeBody;

    /// <summary>
    /// Ancho relativo según el texto real (encabezado + celdas), con piso por tipo de columna.
    /// </summary>
    public static float[] ComputeContentWeights(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs)
    {
        var colCount = Math.Max(1, headers.Count);
        var weights = new float[colCount];
        var sampleCount = Math.Min(rows.Count, 80);

        for (var c = 0; c < colCount; c++)
        {
            var spec = columnSpecs != null && c < columnSpecs.Count ? columnSpecs[c] : null;
            var header = c < headers.Count ? headers[c] : string.Empty;
            var align = spec?.Align ?? GuessAlign(header);
            var maxChars = HeaderDisplayLength(header);

            for (var r = 0; r < sampleCount; r++)
            {
                var raw = c < rows[r].Count ? rows[r][c] : string.Empty;
                var formatted = FormatDisplayCell(raw, spec);
                if (formatted.Length > maxChars)
                    maxChars = formatted.Length;
            }

            var chars = Math.Clamp(maxChars, 4, 40);
            var weight = chars / 9f;
            var headerOrCsv = spec?.CsvName ?? header;
            var isDocument = CredixPdfCellText.IsDocumentColumn(headerOrCsv)
                || CredixPdfCellText.IsDocumentColumn(header);

            if (isDocument)
                weight = Math.Max(weight, 1.2f);
            else if (align == CredixColumnAlign.Right)
                weight = Math.Max(weight, 0.88f);
            else if (align == CredixColumnAlign.Center)
                weight = Math.Max(weight, 0.95f);
            else
                weight = Math.Max(weight, 1.05f);

            if (spec is not null)
                weight = Math.Max(weight, Math.Min(spec.RelativeWeight, 2.4f));

            weights[c] = Math.Clamp(weight, 0.55f, 3.4f);
        }

        return weights;
    }

    private static int HeaderDisplayLength(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return 0;

        return header
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Length)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static CredixColumnAlign ColumnAlign(
        int index,
        IReadOnlyList<string> headers,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs)
    {
        if (columnSpecs != null && index < columnSpecs.Count)
            return columnSpecs[index].Align;

        var header = index < headers.Count ? headers[index] : string.Empty;
        return GuessAlign(header);
    }

    public static string FormatDisplayCell(string? raw, CredixLegacyColumnSpec? spec)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var value = raw.Trim();
        var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date.TimeOfDay == TimeSpan.Zero
                ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : date.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        // Cobro diario RDLC: columna "%" (tasa), no importe monetario.
        if (spec?.DisplayLabel == "%"
            && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
        {
            return rate.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        if (LooksLikeMoney(spec?.CsvName ?? spec?.DisplayLabel)
            && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount.ToString("N2", CultureInfo.GetCultureInfo("es-PE"));
        }

        return value;
    }

    private static bool LooksLikeMoney(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var n = name.Trim();
        if (n.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Nro", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Dias", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Días", StringComparison.OrdinalIgnoreCase)
            || n.Equals("TotalCreditos", StringComparison.OrdinalIgnoreCase)
            || n.Equals("DiasInactividad", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Nro", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Numero", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Cuotas", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Cantidad", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return n.Contains("Monto", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Saldo", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Importe", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Entrada", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Salida", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Mora", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Interes", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Interés", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Total", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Precio", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Deuda", StringComparison.OrdinalIgnoreCase)
            || n.Equals("GA", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Capital", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Tope", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Pagado", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Rentabilidad", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Descuento", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Cobrado", StringComparison.OrdinalIgnoreCase);
    }

    public static CredixColumnAlign GuessAlign(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return CredixColumnAlign.Left;

        var h = header.Trim();
        if (LooksLikeMoney(h) || h.Contains('%', StringComparison.Ordinal))
            return CredixColumnAlign.Right;

        if (LooksCentered(h))
            return CredixColumnAlign.Center;

        return CredixColumnAlign.Left;
    }

    private static bool LooksCentered(string h)
    {
        return h.Contains("Fecha", StringComparison.OrdinalIgnoreCase)
            || h.Contains("DNI", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Dni", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Documento", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Código", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Codigo", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Estado", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Celular", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Serie", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Nro", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Ord.", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Orden", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Cred", StringComparison.OrdinalIgnoreCase)
            || h.Contains("N° créd", StringComparison.OrdinalIgnoreCase)
            || h.Contains("N° cred", StringComparison.OrdinalIgnoreCase)
            || h.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Año", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Anio", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Mes", StringComparison.OrdinalIgnoreCase)
            || h.Contains("SBS", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Días", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Dias", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Cuotas", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Forma pago", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Tipo", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Tipo doc.", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Tipo pago", StringComparison.OrdinalIgnoreCase)
            || h.Equals("Cód. op.", StringComparison.OrdinalIgnoreCase)
            || h.StartsWith("N° ", StringComparison.OrdinalIgnoreCase)
            || h.StartsWith("F.", StringComparison.OrdinalIgnoreCase)
            || h.Equals("RUC", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatMetadataLabel(string label)
    {
        var t = label.Trim();
        if (t.Length == 0)
            return string.Empty;
        return t.EndsWith(':') ? t + " " : t.EndsWith(": ") ? t : t + ": ";
    }

    private static void TryAddDistinctColumn(
        List<MetadataLine> lines,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        string label,
        params string[] headerAliases)
    {
        if (lines.Any(l => l.Label.StartsWith(label.TrimEnd(' ', ':'), StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(l.Value)))
        {
            return;
        }

        var index = -1;
        for (var i = 0; i < headers.Count; i++)
        {
            var h = headers[i].Replace('\n', ' ').Trim();
            if (headerAliases.Any(a => h.Equals(a, StringComparison.OrdinalIgnoreCase)))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return;

        var values = rows
            .Select(r => index < r.Count ? r[index]?.Trim() : null)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        if (values.Count == 0)
            return;

        var text = values.Count <= 2
            ? string.Join(", ", values)
            : $"{values.Count} valores";
        lines.Add(new MetadataLine(label, text!));
    }

    private static Func<IContainer, IContainer> HeaderCellFor(CredixColumnAlign align) =>
        align switch
        {
            CredixColumnAlign.Right => HeaderCellRight,
            CredixColumnAlign.Center => HeaderCellCenter,
            _ => HeaderCellLeft,
        };

    private static Func<IContainer, IContainer> BodyCellFor(CredixColumnAlign align) =>
        align switch
        {
            CredixColumnAlign.Center => BodyCellCenter,
            CredixColumnAlign.Right => BodyCellNumeric,
            _ => BodyCell,
        };

    public static IContainer HeaderCell(IContainer container) =>
        HeaderCellCenter(container);

    public static IContainer HeaderCellCenter(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(HeaderBorderPt)
            .BorderColor(Colors.White)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle()
            .AlignCenter();

    public static IContainer HeaderCellLeft(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(HeaderBorderPt)
            .BorderColor(Colors.White)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle()
            .AlignLeft();

    public static IContainer HeaderCellRight(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(HeaderBorderPt)
            .BorderColor(Colors.White)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle()
            .AlignRight();

    public static IContainer BodyCell(IContainer container) =>
        container
            .Border(BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle();

    public static IContainer BodyCellCenter(IContainer container) =>
        BodyCell(container).AlignCenter();

    public static IContainer BodyCellNumeric(IContainer container) =>
        BodyCell(container).AlignRight();

    /// <summary>Subfila de contacto bajo cada registro (cliente / celular / dirección).</summary>
    public static IContainer BodyCellContactBand(IContainer container) =>
        container
            .Background(CredixReportTokens.MetaBg)
            .Border(BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(3)
            .PaddingHorizontal(4);
}
