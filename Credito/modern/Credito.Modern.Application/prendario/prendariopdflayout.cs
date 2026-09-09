using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Celdas con borde del RDLC impreso (Anexo A / B / acta).</summary>
internal static class PrendarioPdfLayout
{
    private static readonly Color Line = Colors.Black;
    private static readonly Color HeaderBg = Color.FromHex("#DDDDDD");

    public static void Encabezado(ColumnDescriptor col, string titulo, string numeroContrato)
    {
        var logo = CredixReportAssets.LoadLogo();
        col.Item().Row(row =>
        {
            row.ConstantItem(90).Height(36).Image(logo).FitArea();
            row.RelativeItem().AlignMiddle().AlignCenter().Text(titulo).Bold().FontSize(13);
            row.ConstantItem(150).AlignMiddle().Border(0.6f).BorderColor(Line).Padding(4).AlignCenter()
                .Text("Contrato No. " + numeroContrato).SemiBold().FontSize(10);
        });
        col.Item().PaddingTop(4).LineHorizontal(0.6f).LineColor(Line);
    }

    public static void EncabezadoFecha(TableDescriptor table, string emision, string plazo, string vencimiento)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn();
            c.RelativeColumn();
            c.RelativeColumn();
        });
        table.Cell().Element(c => GrayCenter(c, "Fecha de emisión"));
        table.Cell().Element(c => GrayCenter(c, "Plazo"));
        table.Cell().Element(c => GrayCenter(c, "Vencimiento"));
        table.Cell().Element(c => WhiteCenter(c, emision));
        table.Cell().Element(c => WhiteCenter(c, plazo));
        table.Cell().Element(c => WhiteCenter(c, vencimiento));
    }

    public static void Par(ColumnDescriptor col, string labelIzq, string valorIzq, string labelDer, string valorDer)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            table.Cell().Element(c => LabelValue(c, labelIzq, valorIzq));
            table.Cell().Element(c => LabelValue(c, labelDer, valorDer));
        });
    }

    public static void Entero(ColumnDescriptor col, string label, string valor) =>
        col.Item().Element(c => LabelValue(c, label, valor));

    public static void Banda(IContainer c, string titulo) =>
        c.Border(0.6f).BorderColor(Line).Background(HeaderBg).Padding(3).AlignCenter()
            .Text(titulo).SemiBold().FontSize(9);

    public static void FilaMonto(TableDescriptor table, string label, string importe, string letras)
    {
        table.Cell().Element(c => LabelValue(c, label, string.Empty));
        table.Cell().Element(c => WhitePad(c).AlignRight().Text(importe).FontSize(9));
        table.Cell().Element(c => WhitePad(c).AlignCenter().Text(letras).FontSize(8));
    }

    public static void FilaFecha(TableDescriptor table, string label, string fecha)
    {
        table.Cell().Element(c => LabelValue(c, label, string.Empty));
        table.Cell().ColumnSpan(2).Element(c => WhitePad(c).AlignRight().Text(fecha).FontSize(9));
    }

    public static void ColumnasMonto(TableDescriptor table) =>
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(2.3f);
            c.RelativeColumn(1.1f);
            c.RelativeColumn(2.6f);
        });

    public static void Firmas(ColumnDescriptor col)
    {
        col.Item().PaddingTop(36).Row(row =>
        {
            Firma(row, "CLIENTE");
            Firma(row, "CAJA");
            Firma(row, "EJECUTIVO DE CRÉDITOS");
        });
    }

    private static void Firma(RowDescriptor row, string titulo)
    {
        row.RelativeItem().AlignCenter().Column(c =>
        {
            c.Item().Width(170).BorderTop(0.7f).BorderColor(Line);
            c.Item().PaddingTop(4).AlignCenter().Text(titulo).FontSize(9);
        });
    }

    private static void LabelValue(IContainer c, string label, string valor)
    {
        c.Border(0.6f).BorderColor(Line).Padding(3).Text(text =>
        {
            text.Span(label + (label.EndsWith(':') ? " " : ": ")).SemiBold().FontSize(8);
            text.Span(valor).FontSize(9);
        });
    }

    private static void GrayCenter(IContainer c, string text) =>
        c.Border(0.6f).BorderColor(Line).Background(HeaderBg).Padding(3).AlignCenter()
            .Text(text).SemiBold().FontSize(8);

    private static void WhiteCenter(IContainer c, string text) =>
        WhitePad(c).AlignCenter().Text(text).FontSize(9);

    private static IContainer WhitePad(IContainer c) =>
        c.Border(0.6f).BorderColor(Line).Padding(3);
}
