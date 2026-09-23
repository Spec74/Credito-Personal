using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Branding y bloques reutilizables para PDFs de consulta crédito
/// (estado, plan de pagos, movimientos, ficha cliente).
/// Delega tokens a <see cref="CredixReportTokens"/>.
/// </summary>
public static class CreditoPdfBranding
{
    public static readonly string BrandHex = CredixReportTokens.BrandHex;
    public static readonly Color BrandColor = CredixReportTokens.Brand;
    public static readonly Color HeaderBg = CredixReportTokens.TableHeader;
    public static readonly Color MetaBg = CredixReportTokens.MetaBg;
    public static readonly Color Border = CredixReportTokens.Border;
    public static readonly Color Zebra = CredixReportTokens.Zebra;
    public static readonly Color TotalsBg = CredixReportTokens.TotalsBg;

    public static readonly CultureInfo Pe = CredixReportTokens.Pe;
    public static readonly CultureInfo Inv = CredixReportTokens.Inv;

    public static void ComposeTitleBlock(
        ColumnDescriptor col,
        string title,
        string? subtitle = null)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = CredixReportTokens.NowPrinted();

        col.Item().Element(c => CredixLegacyPdfDocument.ComposeTitleBand(c, logo, title, printedAt));

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            col.Item()
                .PaddingTop(3)
                .AlignCenter()
                .Text(subtitle)
                .FontSize(8)
                .FontColor(Colors.Grey.Darken2);
        }
    }

    public static void ComposeMetaGrid(
        IContainer container,
        IReadOnlyList<(string Label, string Value)> fields,
        int columns = 3)
    {
        var items = fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => new CredixLegacyPdfDocument.MetadataLine(f.Label, f.Value))
            .ToList();
        if (items.Count == 0)
        {
            return;
        }

        if (columns == 2)
        {
            container
                .Background(MetaBg)
                .Border(0.6f)
                .BorderColor(Border)
                .PaddingVertical(5)
                .PaddingHorizontal(7)
                .Column(col =>
                {
                    for (var i = 0; i < items.Count; i += 2)
                    {
                        var slice = items.Skip(i).Take(2).ToList();
                        col.Item().PaddingTop(i == 0 ? 0 : 2).Row(row =>
                        {
                            foreach (var line in slice)
                            {
                                row.RelativeItem().Text(t =>
                                {
                                    t.Span($"{line.Label}: ").Bold().FontSize(CredixReportTokens.FontSizeMeta);
                                    t.Span(line.Value.Trim()).FontSize(CredixReportTokens.FontSizeMeta);
                                });
                            }

                            if (slice.Count == 1)
                            {
                                row.RelativeItem();
                            }
                        });
                    }
                });
            return;
        }

        CredixLegacyPdfDocument.ComposeMetadataBlock(container, items);
    }

    public static void ComposeSectionTitle(IContainer container, string title) =>
        container
            .PaddingBottom(3)
            .BorderBottom(1)
            .BorderColor(HeaderBg)
            .Text(title)
            .Bold()
            .FontSize(9)
            .FontColor(BrandColor);

    public static IContainer TableHeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(7))
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .Border(0.5f)
            .BorderColor(Border)
            .Background(HeaderBg)
            .AlignMiddle();

    public static IContainer TableBodyCell(IContainer c, bool zebra = false) =>
        c.PaddingVertical(2)
            .PaddingHorizontal(2)
            .BorderBottom(0.25f)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(zebra ? Zebra : Colors.White)
            .AlignMiddle();

    public static IContainer TableTotalCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(7.5f))
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .Background(TotalsBg)
            .BorderTop(0.8f)
            .BorderColor(BrandColor)
            .AlignMiddle();

    public static void ComposeFooter(IContainer container) =>
        CredixReportTokens.ComposeStandardFooter(container);

    public static string Money(decimal? value) => CredixReportTokens.FormatMoney(value);

    public static string Money(decimal value) => CredixReportTokens.FormatMoney(value);

    public static string DateShort(DateTime? value) => CredixReportTokens.FormatDate(value);

    public static string DateShort(DateTime value) => CredixReportTokens.FormatDate(value);

    public static string OrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}
