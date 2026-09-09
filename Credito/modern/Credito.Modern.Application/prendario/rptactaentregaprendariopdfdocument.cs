using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Acta de entrega voluntaria, texto y bloques del RDLC.</summary>
public static class RptActaEntregaPrendarioPdfDocument
{
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
                page.MarginVertical(16);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    PrendarioPdfLayout.Encabezado(col, "ACTA DE ENTREGA VOLUNTARIA", d.NumeroContrato);

                    col.Item().PaddingTop(10).Text(text =>
                    {
                        text.Span("Yo, " + d.ApellidosNombres);
                        text.Span(", identificado(a) con DNI N° " + d.DniCliente);
                        text.Span(", con domicilio en " + PrendarioPdfTexto.Valor(d.Domicilio));
                        text.Span(", distrito de " + PrendarioPdfTexto.Valor(d.Distrito));
                        text.Span(", provincia de " + PrendarioPdfTexto.Valor(d.Provincia));
                        text.Span(", departamento de " + PrendarioPdfTexto.Valor(d.Departamento));
                        text.Span(", de la ciudad de Ayacucho, a los " + PrendarioPdfTexto.Dia(d.FechaContrato));
                        text.Span(" días del mes de " + PrendarioPdfTexto.MesNombre(d.FechaContrato));
                        text.Span(" del " + PrendarioPdfTexto.Anio(d.FechaContrato) + ",");
                    });

                    col.Item().PaddingTop(6).Text(
                        "por medio de la presente, realizo la ENTREGA VOLUNTARIA de la(s) mercadería(s) detallada(s) en el presente documento, en calidad de garantía prendaria, a favor de "
                        + PrendarioPdfTexto.EmpresaActa
                        + ", en virtud del Contrato de Préstamo Prendario N° "
                        + d.NumeroContrato
                        + " que suscribimos con fecha "
                        + PrendarioPdfTexto.FechaCorta(d.FechaContrato)
                        + ", autorizando expresamente a "
                        + PrendarioPdfTexto.EmpresaActa
                        + " a ejercer los derechos que le correspondan conforme a dicho contrato, incluyendo, de ser el caso, la custodia, conservación, valorización, venta o disposición de los bienes entregados, de acuerdo con la normativa aplicable.");

                    col.Item().PaddingTop(8).Element(c =>
                        PrendarioPdfLayout.Banda(c, "I. DETALLE DE MERCADERÍAS ENTREGADAS"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1);
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
                            Cuerpo(table, i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            Cuerpo(table, bien.Descripcion);
                            Cuerpo(table, PrendarioPdfTexto.Valor(bien.Marca));
                            Cuerpo(table, bien.Serie);
                            Cuerpo(table, PrendarioPdfTexto.Valor(bien.Color));
                            Cuerpo(table, PrendarioPdfTexto.Valor(bien.CodigoInterno));
                            table.Cell().Border(0.6f).BorderColor(Colors.Black).Padding(3).AlignRight()
                                .Text(bien.ValorTasacion.ToString("N2", PrendarioPdfTexto.Cultura));
                            i++;
                        }
                    });

                    col.Item().PaddingTop(8).Element(c =>
                        PrendarioPdfLayout.Banda(c, "II. DECLARACIÓN DEL PRESTATARIO"));
                    col.Item().Border(0.6f).BorderColor(Colors.Black).Padding(4).Text(
                        "El prestatario declara bajo juramento que los bienes entregados son de su exclusiva propiedad, que se encuentran libres de todo gravamen, carga, embargo, litigio o restricción de cualquier naturaleza, y que la información proporcionada en la presente acta es verdadera, completa y exacta.");

                    col.Item().PaddingTop(8).Element(c => PrendarioPdfLayout.Banda(c, "III. ACEPTACIÓN"));
                    col.Item().Border(0.6f).BorderColor(Colors.Black).Padding(4).Text(
                        "La entrega voluntaria de los bienes descritos se realiza en señal de garantía prendaria por el cumplimiento de la obligación asumida por el prestatario en el Contrato de Préstamo Prendario suscrito con "
                        + PrendarioPdfTexto.EmpresaActa
                        + ". En señal de conformidad, suscribimos la presente acta en dos (02) ejemplares del mismo tenor en la ciudad de Ayacucho, a los "
                        + PrendarioPdfTexto.Dia(d.FechaContrato)
                        + " días del mes de "
                        + PrendarioPdfTexto.MesNombre(d.FechaContrato)
                        + " del "
                        + PrendarioPdfTexto.Anio(d.FechaContrato)
                        + ".");

                    col.Item().PaddingTop(6).Border(0.6f).BorderColor(Colors.Black).Padding(4).Text(text =>
                    {
                        text.Span("NOTA: ").Bold();
                        text.Span("La presente acta no constituye novación ni modificación de la obligación contraída. Es parte integrante del Contrato de Préstamo Prendario suscrito entre las partes.");
                    });

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        FirmaActa(row, "PRESTATARIO", "DNI N°: " + d.DniCliente, "FIRMA");
                        FirmaActa(row, "REPRESENTANTE DE " + PrendarioPdfTexto.EmpresaActa, string.Empty, "FIRMA Y SELLO");
                        row.ConstantItem(100).Column(c =>
                        {
                            c.Item().Element(x => PrendarioPdfLayout.Banda(x, "HUELLA DIGITAL"));
                            c.Item().Height(58).Border(0.6f).BorderColor(Colors.Black);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Cabecera(TableDescriptor table, string titulo) =>
        table.Cell().Element(c => PrendarioPdfLayout.Banda(c, titulo));

    private static void Cuerpo(TableDescriptor table, string valor) =>
        table.Cell().Border(0.6f).BorderColor(Colors.Black).Padding(3).Text(valor).FontSize(8);

    private static void FirmaActa(RowDescriptor row, string titulo, string linea, string pie)
    {
        row.RelativeItem().PaddingRight(8).Column(c =>
        {
            c.Item().Element(x => PrendarioPdfLayout.Banda(x, titulo));
            c.Item().Height(46);
            c.Item().BorderTop(0.7f).BorderColor(Colors.Black);
            if (!string.IsNullOrWhiteSpace(linea))
            {
                c.Item().PaddingTop(3).AlignCenter().Text(linea).FontSize(8);
            }

            c.Item().AlignCenter().Text(pie).FontSize(8);
        });
    }
}
