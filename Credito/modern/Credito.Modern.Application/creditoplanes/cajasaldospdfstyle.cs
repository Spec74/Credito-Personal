using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Estilo compartido de los PDF del módulo Saldos (paridad visual Credix + RDLC).</summary>
internal static class CajaSaldosPdfStyle
{
    public static readonly Color Brand = Color.FromHex("#114885");
    public static readonly Color HeaderBg = Color.FromHex("#0F5F8F");
    public static readonly Color BandBg = Color.FromHex("#EAF4FB");
    public static readonly Color Border = Color.FromHex("#B7C7D6");
    public static readonly Color Zebra = Color.FromHex("#F8FBFD");
    public static readonly Color TotalsBg = Color.FromHex("#D6E6F5");

    public static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");

    public static string Money(decimal value) => value.ToString("N2", Pe);

    public static string DateTime(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", Pe);

    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", Pe);

    public static IContainer HeaderCell(IContainer c) =>
        c.Background(HeaderBg)
            .Border(0.5f)
            .BorderColor(Border)
            .PaddingVertical(3)
            .PaddingHorizontal(3)
            .AlignMiddle();

    public static IContainer BodyCell(IContainer c, Color bg) =>
        c.Background(bg)
            .Border(0.4f)
            .BorderColor(Border)
            .PaddingVertical(2)
            .PaddingHorizontal(3)
            .AlignMiddle();

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

    public static void Footer(PageDescriptor page, int filas)
    {
        page.Footer().PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("Filas: ").FontColor(Colors.Grey.Darken2);
                t.Span(filas.ToString(CultureInfo.InvariantCulture)).Bold();
            });
            row.RelativeItem().AlignCenter().Text(t =>
            {
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
            row.RelativeItem().AlignRight()
                .Text("CreditConfiable")
                .FontColor(Colors.Grey.Darken2);
        });
    }
}
