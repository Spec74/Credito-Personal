using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Comprobante de devolución del bien (inverso del acta de entrega).</summary>
public static class RptDevolucionPrendarioPdfDocument
{
    private const float Interlineado = 1.38f;

    static RptDevolucionPrendarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(PrendarioDevolucionDto d)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(18);
                page.MarginVertical(14);
                page.DefaultTextStyle(x => x.FontSize(9).LineHeight(Interlineado));

                page.Content().Column(col =>
                {
                    PrendarioPdfLayout.EncabezadoDevolucion(col, d.NumeroContrato);

                    col.Item().PaddingTop(12).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).LineHeight(Interlineado));
                        text.Justify();
                        text.Span("Se deja constancia de que el (la) sr(a). ");
                        text.Span(d.ApellidosNombres).SemiBold();
                        text.Span(", identificado(a) con DNI N° ");
                        text.Span(d.DniCliente).SemiBold();
                        text.Span(", con domicilio en " + PrendarioPdfTexto.Valor(d.Domicilio));
                        text.Span(", distrito de " + PrendarioPdfTexto.Valor(d.Distrito));
                        text.Span(", provincia de " + PrendarioPdfTexto.Valor(d.Provincia));
                        text.Span(", departamento de " + PrendarioPdfTexto.Valor(d.Departamento));
                        text.Span(", recibe de ");
                        text.Span(PrendarioPdfTexto.EmpresaActa).SemiBold();
                        text.Span(" el bien que se detalla, previamente entregado en garantía del Contrato de Préstamo Prendario N° ");
                        text.Span(d.NumeroContrato).SemiBold();
                        text.Span(". Fecha de devolución: ");
                        text.Span(PrendarioPdfTexto.FechaCorta(d.FechaDevolucion)).SemiBold();
                        text.Span(".");
                    });

                    col.Item().PaddingTop(10).Element(c =>
                        PrendarioPdfLayout.BandaActa(c, "I. BIEN DEVUELTO"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.3f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.1f);
                        });
                        Cabecera(table, "Descripción");
                        Cabecera(table, "Marca");
                        Cabecera(table, "Serie / IMEI");
                        Cabecera(table, "Color");
                        Cabecera(table, "Código");
                        Cabecera(table, "Valor S/.");
                        Cuerpo(table, d.Bien.Descripcion);
                        Cuerpo(table, PrendarioPdfTexto.Valor(d.Bien.Marca));
                        Cuerpo(table, d.Bien.Serie);
                        Cuerpo(table, PrendarioPdfTexto.Valor(d.Bien.Color));
                        Cuerpo(table, PrendarioPdfTexto.Valor(d.Bien.CodigoInterno), true);
                        table.Cell().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).Padding(4)
                            .AlignMiddle().AlignRight()
                            .Text(d.Bien.ValorTasacion.ToString("N2", PrendarioPdfTexto.Cultura)).FontSize(8);
                    });

                    col.Item().PaddingTop(10).Element(c =>
                        PrendarioPdfLayout.BandaActa(c, "II. ESTADO"));
                    col.Item().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).PaddingVertical(7).PaddingHorizontal(8)
                        .Text(text =>
                        {
                            text.Span("Estado del bien: ").SemiBold();
                            text.Span(d.Estado);
                            text.Span(". El prestatario declara recibir el bien a su entera satisfacción.");
                        });

                    col.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Cell().PaddingRight(10).Element(c =>
                            c.MinHeight(30).Element(x => PrendarioPdfLayout.BandaActa(x, "PRESTATARIO")));
                        table.Cell().Element(c =>
                            c.MinHeight(30).Element(x =>
                                PrendarioPdfLayout.BandaActa(x, "REPRESENTANTE DE " + PrendarioPdfTexto.EmpresaActa)));
                        table.Cell().PaddingRight(10).Element(c => BloqueFirma(c, "DNI N°: " + d.DniCliente, "FIRMA"));
                        table.Cell().Element(c => BloqueFirma(c, string.Empty, "FIRMA Y SELLO"));
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Cabecera(TableDescriptor table, string titulo) =>
        table.Cell().Element(c => PrendarioPdfLayout.BandaActa(c, titulo));

    private static void Cuerpo(TableDescriptor table, string valor, bool centrar = false)
    {
        var cell = table.Cell().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).Padding(4).AlignMiddle();
        if (centrar)
        {
            cell.AlignCenter();
        }

        cell.Text(valor).FontSize(8);
    }

    private static void BloqueFirma(IContainer c, string linea, string pie)
    {
        c.Column(col =>
        {
            col.Item().Height(34);
            PrendarioPdfLayout.LineaFirma(col, 150);
            if (!string.IsNullOrWhiteSpace(linea))
            {
                col.Item().PaddingTop(4).AlignCenter().Text(linea).FontSize(8);
            }
            else
            {
                col.Item().Height(12);
            }

            col.Item().AlignCenter().Text(pie).FontSize(8);
        });
    }
}
