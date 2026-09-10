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
    public static readonly Color ActaAzul = Color.FromHex("#053B8A");

    public static void Encabezado(
        ColumnDescriptor col,
        string titulo,
        string numeroContrato,
        string? subtitulo = null)
    {
        var logo = CredixReportAssets.LoadLogo();
        col.Item().Row(row =>
        {
            row.ConstantItem(88).Height(38).Image(logo).FitArea();
            row.RelativeItem().AlignMiddle().AlignCenter().Column(t =>
            {
                t.Item().AlignCenter().Text(titulo).Bold().FontSize(12);
                if (!string.IsNullOrWhiteSpace(subtitulo))
                {
                    t.Item().AlignCenter().Text(subtitulo).Bold().FontSize(12);
                }
            });
            row.ConstantItem(148).AlignMiddle().Border(0.75f).BorderColor(Line).PaddingVertical(5).PaddingHorizontal(6)
                .AlignCenter().Text("Contrato No. " + numeroContrato).SemiBold().FontSize(10);
        });
    }

    public static void EncabezadoFecha(ColumnDescriptor col, string emision, string plazo, string vencimiento)
    {
        col.Item().PaddingTop(8).AlignCenter().Width(340).Table(table =>
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
        });
    }

    public static void EncabezadoActa(ColumnDescriptor col, string numeroContrato)
    {
        Encabezado(col, "ACTA DE ENTREGA VOLUNTARIA", numeroContrato);
        col.Item().PaddingTop(4).AlignCenter().Width(168).Background(ActaAzul)
            .PaddingVertical(3).AlignCenter()
            .Text("CRÉDITOS PRENDARIOS").FontColor(Colors.White).SemiBold().FontSize(8);
    }

    public static void Par(ColumnDescriptor col, string labelIzq, string valorIzq, string labelDer, string valorDer)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.55f);
                c.RelativeColumn(1);
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

    public static void BandaActa(IContainer c, string titulo) =>
        c.Border(0.6f).BorderColor(ActaAzul).Background(ActaAzul).Padding(4).AlignCenter()
            .Text(titulo).FontColor(Colors.White).SemiBold().FontSize(8);

    public static void FilaMonto(TableDescriptor table, string label, string importe, string letras)
    {
        table.Cell().Element(c => LabelValue(c, label, importe));
        table.Cell().Element(c => WhitePad(c).AlignCenter().Text(letras).FontSize(8));
    }

    public static void FilaFecha(TableDescriptor table, string label, string fecha)
    {
        table.Cell().Element(c => LabelValue(c, label, string.Empty));
        table.Cell().Element(c => WhitePad(c).AlignRight().Text(fecha).FontSize(9));
    }

    public static void ColumnasMonto(TableDescriptor table) =>
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(1.35f);
            c.RelativeColumn(1.65f);
        });

    public static void FilaPrenda(TableDescriptor table, string label, string valor, int minHeight = 16)
    {
        table.Cell().Element(c =>
            c.Border(0.6f).BorderColor(Line).Background(HeaderBg).Padding(3).MinHeight(minHeight)
                .AlignMiddle().Text(label + ":").SemiBold().FontSize(8));
        table.Cell().Element(c =>
            c.Border(0.6f).BorderColor(Line).Padding(3).MinHeight(minHeight)
                .AlignMiddle().Text(valor).FontSize(9));
    }

    public static void MontoPrenda(IContainer c, decimal monto) =>
        c.Border(0.6f).BorderColor(Line).AlignMiddle().AlignCenter()
            .Text(text =>
            {
                text.Span("S/  ").FontSize(9);
                text.Span(monto.ToString("N2", PrendarioPdfTexto.Cultura)).FontSize(10);
            });

    public static void MontoTotal(IContainer c, decimal monto) =>
        c.Border(0.6f).BorderColor(Line).Padding(3).AlignCenter()
            .Text(monto.ToString("N2", PrendarioPdfTexto.Cultura)).FontSize(9);

    public static void LineaFirma(ColumnDescriptor col, float ancho = 160)
    {
        col.Item().AlignCenter().Width(ancho).Height(0.9f).Background(Colors.Black);
    }

    public static void Firmas(ColumnDescriptor col)
    {
        col.Item().PaddingTop(28).Row(row =>
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
            c.Item().Height(36);
            c.Item().AlignCenter().Width(175).Height(0.9f).Background(Line);
            c.Item().PaddingTop(5).AlignCenter().Text(titulo).FontSize(9);
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
