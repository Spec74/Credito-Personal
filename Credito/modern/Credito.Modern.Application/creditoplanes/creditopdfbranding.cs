using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Helpers de dominio crédito sobre el shell <see cref="CredixReportPage"/> /
/// tokens <see cref="CredixReportTokens"/>. No inventar colores ni márgenes aquí.
/// </summary>
public static class CreditoPdfBranding
{
    public static readonly string BrandHex = CredixReportTokens.BrandHex;
    public static readonly Color BrandColor = CredixReportTokens.Brand;
    public static readonly Color HeaderBg = CredixReportTokens.TableHeaderStrong;
    public static readonly Color MetaBg = CredixReportTokens.SoftBg;
    public static readonly Color Border = CredixReportTokens.BorderSoft;
    public static readonly Color Zebra = CredixReportTokens.Zebra;
    public static readonly Color TotalsBg = CredixReportTokens.TotalsBg;

    public const float PageMarginPt = CredixReportTokens.PageMarginPortraitPt;

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

        if (!string.IsNullOrWhiteSpace(subtitle)
            && !string.Equals(
                subtitle.Trim(),
                CredixReportTokens.CompanyLegalName,
                StringComparison.OrdinalIgnoreCase))
        {
            col.Item()
                .PaddingTop(3)
                .AlignCenter()
                .Text(subtitle.Trim())
                .FontSize(8)
                .FontColor(Colors.Grey.Darken2);
        }
    }

    public static void ComposePageHeader(
        IContainer container,
        string title,
        IReadOnlyList<(string Label, string Value)> fields,
        int metaColumns = 3) =>
        CredixReportPage.ComposeHeader(container, title, fields, metaColumns);

    public static void ComposeMetaGrid(
        IContainer container,
        IReadOnlyList<(string Label, string Value)> fields,
        int columns = 3) =>
        CredixReportPage.ComposeMeta(container, fields, columns);

    public static void ComposeFirmas(IContainer container) =>
        CredixLegacyPdfDocument.ComposeSignatureBlock(container);

    public static void ComposeSectionTitle(IContainer container, string title) =>
        container
            .Background(HeaderBg)
            .Border(0.5f)
            .BorderColor(HeaderBg)
            .PaddingVertical(4)
            .PaddingHorizontal(6)
            .Text(title.Trim().ToUpperInvariant())
            .Bold()
            .FontSize(9)
            .FontColor(Colors.White);

    public static IContainer TableHeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(7).FontColor(Colors.White))
            .PaddingVertical(4)
            .PaddingHorizontal(3)
            .Border(0.5f)
            .BorderColor(HeaderBg)
            .Background(HeaderBg)
            .AlignMiddle();

    public static IContainer TableBodyCell(IContainer c, bool zebra = false) =>
        c.DefaultTextStyle(x => x)
            .PaddingVertical(2)
            .PaddingHorizontal(3)
            .BorderLeft(0.4f)
            .BorderRight(0.4f)
            .BorderBottom(0.4f)
            .BorderColor(Border)
            .Background(zebra ? Zebra : Colors.White)
            .AlignMiddle();

    public static IContainer TableTotalCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(7.5f))
            .PaddingVertical(3)
            .PaddingHorizontal(3)
            .Background(TotalsBg)
            .BorderLeft(0.4f)
            .BorderRight(0.4f)
            .BorderBottom(0.5f)
            .BorderColor(Border)
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
