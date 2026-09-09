using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Contrato de crédito prendario (sustituto de <c>rptContratoPrendario.rdlc</c>).</summary>
public static class RptContratoPrendarioPdfDocument
{
    static RptContratoPrendarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(PrendarioContratoDto d)
    {
        var cultura = CultureInfo.GetCultureInfo("es-PE");
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Footer().Element(CreditoPlanes.CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    CreditoPlanes.CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        "CONTRATO DE CRÉDITO PRENDARIO",
                        $"N.º {d.NumeroContrato}");

                    col.Item().Text(text =>
                    {
                        text.Span("En la ciudad, a los ").FontSize(9);
                        text.Span(d.FechaEmision.ToString("dd 'de' MMMM 'de' yyyy", cultura)).Bold();
                        text.Span(", se celebra el presente contrato entre CREDICONFIABLE, en calidad de acreedor prendario, y el(la) cliente que se identifica a continuación.");
                    });

                    col.Item().Element(c => SectionTitle(c, "I. Datos del cliente"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });
                        Field(table, "Cliente", d.ApellidosNombres);
                        Field(table, "DNI", d.DniCliente);
                        Field(table, "Cónyuge", Texto(d.ConyugeNombre));
                        Field(table, "DNI cónyuge", Texto(d.ConyugeDni));
                        Field(table, "Celular", Texto(d.Celular));
                        Field(table, "Correo", Texto(d.Correo));
                        Field(table, "Domicilio", Texto(d.Domicilio), span: 3);
                        Field(table, "Distrito", Texto(d.Distrito));
                        Field(table, "Referencia", Texto(d.Referencia), span: 3);
                    });

                    col.Item().Element(c => SectionTitle(c, "II. Condiciones del crédito"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });
                        Field(table, "Préstamo", Money(d.MontoPrestamo, cultura));
                        Field(table, "En letras", NumeroALetras.EnSoles(d.MontoPrestamo), span: 3);
                        Field(table, "Tasación", Money(d.MontoTasacion, cultura));
                        Field(table, "En letras", NumeroALetras.EnSoles(d.MontoTasacion), span: 3);
                        Field(table, "Tasa mensual", $"{d.TasaInteres:0.##} %");
                        Field(table, "Interés mensual", Money(d.InteresMensual, cultura));
                        Field(table, "Interés diario", Money(d.InteresDiario, cultura));
                        Field(table, "Gastos adm.", Money(d.MontoGastosAdm, cultura));
                        Field(table, "Plazo", d.PlazoTexto);
                        Field(table, "Vencimiento", d.FechaVencimiento.ToString("dd/MM/yyyy", cultura));
                        Field(table, "Remate", d.FechaRemate.ToString("dd/MM/yyyy", cultura));
                        Field(table, "Desembolso", d.FechaDesembolso.ToString("dd/MM/yyyy", cultura));
                    });

                    col.Item().Element(c => SectionTitle(c, "III. Bienes en custodia"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.4f);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Descripción");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Marca");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Modelo");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Serie");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Color");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Tasación");
                        });
                        foreach (var bien in d.Bienes)
                        {
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(bien.Descripcion);
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(Texto(bien.Marca));
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(Texto(bien.Modelo));
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(bien.Serie);
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(Texto(bien.Color));
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).AlignRight()
                                .Text(Money(bien.ValorTasacion, cultura));
                        }
                    });

                    col.Item().PaddingTop(6).Text(
                        "El cliente declara entregar en prenda los bienes descritos, que permanecen en custodia de la empresa hasta la cancelación total de la obligación o, en su defecto, hasta la fecha de remate. Las cláusulas generales del contrato prendario forman parte integrante de este documento.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingTop(28).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Width(180).BorderTop(0.5f).BorderColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text(d.ApellidosNombres).Bold().FontSize(8);
                            c.Item().Text($"DNI {d.DniCliente}").FontSize(8);
                            c.Item().Text("Cliente").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Width(180).BorderTop(0.5f).BorderColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text(Texto(d.EjecutivoNombre)).Bold().FontSize(8);
                            c.Item().Text("Analista de créditos").FontSize(8);
                            c.Item().Text("Por Crediconfiable").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void SectionTitle(IContainer c, string title) =>
        c.Background(Color.FromHex("#114885")).Padding(4).Text(title).FontColor(Colors.White).SemiBold().FontSize(9);

    private static void Field(TableDescriptor table, string label, string value, int span = 1)
    {
        table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell)
            .Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
        table.Cell().ColumnSpan((uint)span).Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell)
            .Text(value).FontSize(9);
    }

    private static string Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim();

    private static string Money(decimal valor, CultureInfo cultura) =>
        valor.ToString("C2", cultura);
}
