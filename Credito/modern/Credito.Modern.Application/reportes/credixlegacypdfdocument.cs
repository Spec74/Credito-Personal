using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Layout PDF alineado a informes RDLC legacy: logo, metadatos, tabla con encabezado
/// LightSteelBlue, bordes gruesos en cabecera y pie «Página X de Y».
/// </summary>
public static class CredixLegacyPdfDocument
{
    public const float HeaderBorderPt = 1.25f;
    public const float BodyBorderPt = 0.5f;
    public const float FontSizeBody = 7f;
    public const float FontSizeTitle = 10f;

    private static readonly Color HeaderBg = Color.FromHex("#B0C4DE");
    private static readonly Color BorderColor = Color.FromHex("#808080");

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
        bool Landscape = true,
        int? RowCountFooter = null,
        IReadOnlyList<CredixLegacyColumnSpec>? ColumnSpecs = null);

    public static byte[] FromTable(ReportTable report)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        var inv = CultureInfo.InvariantCulture;
        var rowCount = report.RowCountFooter ?? report.Rows.Count;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                if (report.Landscape)
                    page.Size(PageSizes.A4.Landscape());
                else
                    page.Size(PageSizes.A4);

                page.Margin(18);
                page.DefaultTextStyle(s => s.FontSize(FontSizeBody));

                page.Header().Column(col =>
                {
                    col.Item().Element(c => ComposeTitleBand(c, logo, report.Title, printedAt));
                    if (report.Metadata.Count > 0)
                    {
                        col.Item().PaddingTop(4)
                            .Element(c => ComposeMetadataBlock(c, report.Metadata));
                    }
                });

                page.Content().PaddingTop(6).Element(c =>
                    ComposeDataTable(c, report.Headers, report.Rows, report.ColumnSpecs));

                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignLeft().Text(t =>
                    {
                        t.Span("Filas: ");
                        t.Span(rowCount.ToString(inv)).Bold();
                    });
                    row.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    public static void ComposeTitleBand(
        IContainer container,
        byte[] logo,
        string title,
        string printedAt)
    {
        container.Row(row =>
        {
            row.ConstantItem(88).Height(46).Image(logo).FitArea();
            row.RelativeItem().AlignMiddle().AlignCenter().Text(title).Bold().FontSize(FontSizeTitle);
            row.ConstantItem(120).AlignMiddle().AlignRight().Text(printedAt).FontSize(8);
        });
    }

    public static void ComposeMetadataBlock(
        IContainer container,
        IReadOnlyList<MetadataLine> metadata)
    {
        container.Column(col =>
        {
            foreach (var line in metadata)
            {
                col.Item().Text(t =>
                {
                    t.Span(line.Label).Bold();
                    t.Span(line.Value);
                });
            }
        });
    }

    public static void ComposeDataTable(
        IContainer container,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs = null)
    {
        var colCount = Math.Max(1, headers.Count);
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (var c = 0; c < colCount; c++)
                {
                    var weight = ColumnWeight(c, headers, columnSpecs);
                    columns.RelativeColumn(weight);
                }
            });

            table.Header(header =>
            {
                for (var c = 0; c < colCount; c++)
                {
                    var label = c < headers.Count ? headers[c] : string.Empty;
                    var align = ColumnAlign(c, columnSpecs);
                    header.Cell()
                        .Element(HeaderCellFor(align))
                        .Text(label)
                        .Bold()
                        .FontSize(FontSizeBody);
                }
            });

            foreach (var row in rows)
            {
                for (var c = 0; c < colCount; c++)
                {
                    var cell = c < row.Count ? row[c] : string.Empty;
                    var align = ColumnAlign(c, columnSpecs);
                    table.Cell()
                        .Element(BodyCellFor(align))
                        .Text(cell ?? string.Empty)
                        .FontSize(FontSizeBody);
                }
            }
        });
    }

    private static float ColumnWeight(
        int index,
        IReadOnlyList<string> headers,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs)
    {
        if (columnSpecs != null && index < columnSpecs.Count)
            return Math.Max(0.4f, columnSpecs[index].RelativeWeight);

        var header = index < headers.Count ? headers[index] : string.Empty;
        var align = GuessAlign(header);
        return CredixColumnWeights.For(header, align);
    }

    private static CredixColumnAlign ColumnAlign(
        int index,
        IReadOnlyList<CredixLegacyColumnSpec>? columnSpecs) =>
        columnSpecs != null && index < columnSpecs.Count
            ? columnSpecs[index].Align
            : CredixColumnAlign.Left;

    private static CredixColumnAlign GuessAlign(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return CredixColumnAlign.Left;

        var h = header.Trim();
        if (h.StartsWith("Monto", StringComparison.OrdinalIgnoreCase)
            || h.StartsWith("Saldo", StringComparison.OrdinalIgnoreCase)
            || h.StartsWith("Total", StringComparison.OrdinalIgnoreCase)
            || h.StartsWith('N')
            || h.Contains("Id", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Cuota", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Interes", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Mora", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Precio", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Cant", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Días", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Dias", StringComparison.OrdinalIgnoreCase)
            || h.Contains('%'))
        {
            return CredixColumnAlign.Right;
        }

        return CredixColumnAlign.Left;
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
            .BorderColor(BorderColor)
            .Padding(2)
            .AlignMiddle()
            .AlignCenter();

    public static IContainer HeaderCellLeft(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(HeaderBorderPt)
            .BorderColor(BorderColor)
            .Padding(2)
            .AlignMiddle()
            .AlignLeft();

    public static IContainer HeaderCellRight(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(HeaderBorderPt)
            .BorderColor(BorderColor)
            .Padding(2)
            .AlignMiddle()
            .AlignRight();

    public static IContainer BodyCell(IContainer container) =>
        container
            .Border(BodyBorderPt)
            .BorderColor(BorderColor)
            .Padding(2)
            .AlignMiddle();

    public static IContainer BodyCellCenter(IContainer container) =>
        BodyCell(container).AlignCenter();

    public static IContainer BodyCellNumeric(IContainer container) =>
        BodyCell(container).AlignRight();

    /// <summary>Subfila de contacto bajo cada registro (cliente / celular / dirección).</summary>
    public static IContainer BodyCellContactBand(IContainer container) =>
        container
            .Background(Color.FromHex("#F0F4F8"))
            .Border(BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(3)
            .PaddingHorizontal(4);
}
