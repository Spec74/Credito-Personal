using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Encabezado común QuestPDF (sustituto visual de bandas RDLC en consulta crédito).</summary>
public static class CreditoPdfBranding
{
    public static readonly string BrandHex = "#114885";

    public static void ComposeTitleBlock(
        ColumnDescriptor col,
        string title,
        string? subtitle = null)
    {
        col.Item()
            .Background(Colors.Blue.Darken3)
            .Padding(8)
            .AlignCenter()
            .Text(title)
            .Bold()
            .FontSize(12)
            .FontColor(Colors.White);

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
            .Background(Colors.Grey.Lighten3);

    public static IContainer TableBodyCell(IContainer c) =>
        c.Padding(3).BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten3);
}
