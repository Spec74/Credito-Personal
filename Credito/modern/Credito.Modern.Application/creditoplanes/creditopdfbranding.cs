using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Encabezado común QuestPDF (sustituto visual de bandas RDLC en consulta crédito).</summary>
public static class CreditoPdfBranding
{
    public static readonly string BrandHex = "#114885";
    private static readonly Color BrandColor = Color.FromHex(BrandHex);
    private static readonly Color HeaderBg = Color.FromHex("#B0C4DE");

    public static void ComposeTitleBlock(
        ColumnDescriptor col,
        string title,
        string? subtitle = null)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);

        col.Item()
            .BorderBottom(1.25f)
            .BorderColor(HeaderBg)
            .PaddingBottom(6)
            .Row(row =>
            {
                row.ConstantItem(90).Height(44).Image(logo).FitArea();
                row.RelativeItem().AlignMiddle().Column(titleCol =>
                {
                    titleCol.Item().AlignCenter().Text("Crediconfiable")
                        .Bold()
                        .FontSize(9)
                        .FontColor(BrandColor);
                    titleCol.Item().PaddingTop(2).AlignCenter().Text(title)
                        .Bold()
                        .FontSize(12)
                        .FontColor(Colors.Black);
                });
                row.ConstantItem(120).AlignMiddle().AlignRight().Text(printedAt).FontSize(8);
            });

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            col.Item()
                .PaddingTop(4)
                .AlignCenter()
                .Text(subtitle)
                .FontSize(8)
                .FontColor(Colors.Grey.Darken2);
        }
    }

    public static IContainer TableHeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(8))
            .Padding(3)
            .Border(0.5f)
            .BorderColor(Colors.Grey.Darken1)
            .Background(HeaderBg);

    public static IContainer TableBodyCell(IContainer c) =>
        c.Padding(3).BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten3);

    public static void ComposeFooter(IContainer container)
    {
        container.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2)).Row(row =>
        {
            row.RelativeItem().Text("Crediconfiable");
            row.RelativeItem().AlignCenter().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
            row.RelativeItem().AlignRight().Text("Exportación moderna");
        });
    }
}
