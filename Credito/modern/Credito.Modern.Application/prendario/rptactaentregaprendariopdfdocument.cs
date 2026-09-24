using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Acta de entrega voluntaria, texto y bloques del RDLC.</summary>
public static class RptActaEntregaPrendarioPdfDocument
{
    /// <summary>Interlineado cómodo en A4 sin forzar segunda hoja (hay margen libre bajo firmas).</summary>
    private const float Interlineado = 1.38f;

    static RptActaEntregaPrendarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(PrendarioActaDto d)
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
                    PrendarioPdfLayout.EncabezadoActa(col, d.NumeroContrato);

                    col.Item().PaddingTop(12).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).LineHeight(Interlineado));
                        text.Justify();
                        text.Span("Yo, ");
                        text.Span(d.ApellidosNombres).SemiBold();
                        text.Span(", identificado(a) con DNI N° ");
                        text.Span(d.DniCliente).SemiBold();
                        text.Span(", con domicilio en " + PrendarioPdfTexto.Valor(d.Domicilio));
                        text.Span(", distrito de " + PrendarioPdfTexto.Valor(d.Distrito));
                        text.Span(", provincia de " + PrendarioPdfTexto.Valor(d.Provincia));
                        text.Span(", departamento de " + PrendarioPdfTexto.Valor(d.Departamento));
                        text.Span(", de la ciudad de Ayacucho, a los " + PrendarioPdfTexto.Dia(d.FechaContrato));
                        text.Span(" días del mes de " + PrendarioPdfTexto.MesNombre(d.FechaContrato));
                        text.Span(" del " + PrendarioPdfTexto.Anio(d.FechaContrato) + ",");
                    });

                    col.Item().PaddingTop(8).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).LineHeight(Interlineado));
                        text.Justify();
                        text.Span("por medio de la presente, realizo la ");
                        text.Span("ENTREGA VOLUNTARIA").SemiBold();
                        text.Span(" de la(s) mercadería(s) detallada(s) en el presente documento, en calidad de garantía prendaria, a favor de ");
                        text.Span(PrendarioPdfTexto.EmpresaActa).SemiBold();
                        text.Span(", en virtud del Contrato de Préstamo Prendario N° ");
                        text.Span(d.NumeroContrato).SemiBold();
                        text.Span(" que suscribimos con fecha ");
                        text.Span(PrendarioPdfTexto.FechaCorta(d.FechaContrato)).SemiBold();
                        text.Span(", autorizando expresamente a ");
                        text.Span(PrendarioPdfTexto.EmpresaActa).SemiBold();
                        text.Span(" a ejercer los derechos que le correspondan conforme a dicho contrato, incluyendo, de ser el caso, la custodia, conservación, valorización, venta o disposición de los bienes entregados, de acuerdo con la normativa aplicable.");
                    });

                    col.Item().PaddingTop(10).Element(c =>
                        PrendarioPdfLayout.BandaActa(c, "I. DETALLE DE MERCADERÍAS ENTREGADAS"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24);
                            c.RelativeColumn(2.3f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1.35f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.1f);
                        });
                        Cabecera(table, "N°");
                        Cabecera(table, "Descripción del bien");
                        Cabecera(table, "Marca");
                        Cabecera(table, "Serie / IMEI");
                        Cabecera(table, "Color");
                        Cabecera(table, "Código interno");
                        Cabecera(table, "Valor referencial S/.");
                        var i = 1;
                        foreach (var bien in d.Bienes)
                        {
                            Cuerpo(table, i.ToString(System.Globalization.CultureInfo.InvariantCulture), true);
                            Cuerpo(table, bien.Descripcion);
                            Cuerpo(table, PrendarioPdfTexto.Valor(bien.Marca));
                            Cuerpo(table, bien.Serie);
                            Cuerpo(table, PrendarioPdfTexto.Valor(bien.Color));
                            Cuerpo(table, PrendarioPdfTexto.Valor(bien.CodigoInterno), true);
                            table.Cell().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).Padding(4)
                                .AlignMiddle().AlignRight()
                                .Text(bien.ValorTasacion.ToString("N2", PrendarioPdfTexto.Cultura)).FontSize(8);
                            i++;
                        }
                    });

                    col.Item().PaddingTop(10).Element(c =>
                        PrendarioPdfLayout.BandaActa(c, "II. DECLARACIÓN DEL PRESTATARIO"));
                    col.Item().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).PaddingVertical(7).PaddingHorizontal(8)
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9).LineHeight(Interlineado));
                            text.Justify();
                            text.Span("El prestatario declara bajo juramento que los bienes entregados son de su exclusiva propiedad, que se encuentran libres de todo gravamen, carga, embargo, litigio o restricción de cualquier naturaleza, y que la información proporcionada en la presente acta es verdadera, completa y exacta.");
                        });

                    col.Item().PaddingTop(10).Element(c => PrendarioPdfLayout.BandaActa(c, "III. ACEPTACIÓN"));
                    col.Item().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).PaddingVertical(7).PaddingHorizontal(8)
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9).LineHeight(Interlineado));
                            text.Justify();
                            text.Span("La entrega voluntaria de los bienes descritos se realiza en señal de garantía prendaria por el cumplimiento de la obligación asumida por el prestatario en el Contrato de Préstamo Prendario suscrito con ");
                            text.Span(PrendarioPdfTexto.EmpresaActa).SemiBold();
                            text.Span(". En señal de conformidad, suscribimos la presente acta en dos (02) ejemplares del mismo tenor en la ciudad de Ayacucho, a los ");
                            text.Span(PrendarioPdfTexto.Dia(d.FechaContrato)).SemiBold();
                            text.Span(" días del mes de ");
                            text.Span(PrendarioPdfTexto.MesNombre(d.FechaContrato)).SemiBold();
                            text.Span(" del ");
                            text.Span(PrendarioPdfTexto.Anio(d.FechaContrato)).SemiBold();
                            text.Span(".");
                        });

                    col.Item().PaddingTop(8).Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul)
                        .PaddingVertical(6).PaddingHorizontal(8)
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8.5f).LineHeight(1.32f));
                            text.Justify();
                            text.Span("NOTA: ").Bold();
                            text.Span("La presente acta no constituye novación ni modificación de la obligación contraída. Es parte integrante del Contrato de Préstamo Prendario suscrito entre las partes.");
                        });

                    // Espacio justo para firmas: el interlineado ya consumió el hueco inferior vacío.
                    col.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(118);
                        });
                        table.Cell().PaddingRight(10).Element(c =>
                            c.MinHeight(30).Element(x => PrendarioPdfLayout.BandaActa(x, "PRESTATARIO")));
                        table.Cell().PaddingRight(10).Element(c =>
                            c.MinHeight(30).Element(x =>
                                PrendarioPdfLayout.BandaActa(x, "REPRESENTANTE DE " + PrendarioPdfTexto.EmpresaActa)));
                        table.Cell().Element(c =>
                            c.MinHeight(30).Element(x =>
                                PrendarioPdfLayout.BandaActa(x, "HUELLA DIGITAL DEL PRESTATARIO")));

                        table.Cell().PaddingRight(10).Element(c =>
                            BloqueFirma(c, "DNI N°: " + d.DniCliente, "FIRMA"));
                        table.Cell().PaddingRight(10).Element(c =>
                            BloqueFirma(c, string.Empty, "FIRMA Y SELLO"));
                        table.Cell().Border(0.6f).BorderColor(PrendarioPdfLayout.ActaAzul).MinHeight(78);
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
