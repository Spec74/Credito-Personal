using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Estilo compartido de los PDF del módulo Saldos (tokens <see cref="CredixReportTokens"/>).</summary>
internal static class CajaSaldosPdfStyle
{
    public static readonly Color Brand = CredixReportTokens.Brand;
    public static readonly Color HeaderBg = CredixReportTokens.TableHeaderStrong;
    public static readonly Color BandBg = CredixReportTokens.SoftBg;
    public static readonly Color Border = CredixReportTokens.BorderSoft;
    public static readonly Color Zebra = CredixReportTokens.Zebra;
    public static readonly Color TotalsBg = CredixReportTokens.TotalsBg;

    public static readonly CultureInfo Pe = CredixReportTokens.Pe;

    public static string Money(decimal value) => CredixReportTokens.FormatMoney(value);

    public static string DateTime(DateTime value) => value.ToString(CredixReportTokens.DateTimeFormat, Pe);

    public static string Date(DateTime value) => CredixReportTokens.FormatDate(value);

    public static IContainer HeaderCell(IContainer c) =>
        c.Background(HeaderBg)
            .Border(0.5f)
            .BorderColor(Border)
            .PaddingVertical(3)
            .PaddingHorizontal(3)
            .AlignMiddle()
            .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold());

    public static IContainer BodyCell(IContainer c, Color bg) =>
        c.Background(bg)
            .Border(0.4f)
            .BorderColor(Border)
            .PaddingVertical(2)
            .PaddingHorizontal(3)
            .AlignMiddle()
            .DefaultTextStyle(x => x);

    public static IContainer TotalsCell(IContainer c) =>
        c.Background(TotalsBg)
            .Border(0.5f)
            .BorderColor(Border)
            .PaddingVertical(3)
            .PaddingHorizontal(3)
            .AlignMiddle();

    public static void TitleBand(
        IContainer container,
        byte[] logo,
        string title,
        string printedAt)
    {
        CredixLegacyPdfDocument.ComposeTitleBand(container, logo, title, printedAt);
    }

    public static void Footer(PageDescriptor page, int filas) =>
        page.Footer().Element(c => CredixReportTokens.ComposeStandardFooter(c, filas));
}
