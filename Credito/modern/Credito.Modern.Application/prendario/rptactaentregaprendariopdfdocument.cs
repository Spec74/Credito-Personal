using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>Acta de entrega voluntaria (sustituto de <c>rptActaEntregaPrendario.rdlc</c>).</summary>
public static class RptActaEntregaPrendarioPdfDocument
{
    static RptActaEntregaPrendarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(PrendarioActaDto d)
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
                        "ACTA DE ENTREGA VOLUNTARIA",
                        $"Contrato N.º {d.NumeroContrato}");

                    col.Item().Text(text =>
                    {
                        text.Span("En ");
                        text.Span(Texto(d.Distrito)).Bold();
                        text.Span(", provincia de ");
                        text.Span(Texto(d.Provincia)).Bold();
                        text.Span(", departamento de ");
                        text.Span(Texto(d.Departamento)).Bold();
                        text.Span(", a los ");
                        text.Span(d.FechaContrato.Day.ToString("00", CultureInfo.InvariantCulture)).Bold();
                        text.Span(" días del mes de ");
                        text.Span(d.FechaContrato.ToString("MMMM", cultura)).Bold();
                        text.Span(" de ");
                        text.Span(d.FechaContrato.Year.ToString(CultureInfo.InvariantCulture)).Bold();
                        text.Span(".");
                    });

                    col.Item().Text(text =>
                    {
                        text.Span("Yo, ");
                        text.Span(d.ApellidosNombres).Bold();
                        text.Span(", identificado(a) con DNI ");
                        text.Span(d.DniCliente).Bold();
                        text.Span(", con domicilio en ");
                        text.Span(Texto(d.Domicilio)).Bold();
                        text.Span(", declaro entregar de manera voluntaria a CREDICONFIABLE los bienes que se detallan, en garantía del contrato de crédito prendario N.º ");
                        text.Span(d.NumeroContrato).Bold();
                        text.Span(".");
                    });

                    col.Item().Element(c =>
                        c.Background(Color.FromHex("#114885")).Padding(4)
                            .Text("Bienes entregados en custodia")
                            .FontColor(Colors.White).SemiBold().FontSize(9));

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.RelativeColumn(2.4f);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("N.º");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Descripción");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Marca");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Modelo");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).Text("Serie");
                            h.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Tasación");
                        });
                        var i = 1;
                        foreach (var bien in d.Bienes)
                        {
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(i.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(bien.Descripcion);
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(Texto(bien.Marca));
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(Texto(bien.Modelo));
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).Text(bien.Serie);
                            table.Cell().Element(CreditoPlanes.CreditoPdfBranding.TableBodyCell).AlignRight()
                                .Text(bien.ValorTasacion.ToString("C2", cultura));
                            i++;
                        }
                    });

                    col.Item().PaddingTop(6).Text(
                        "Declaro que los bienes son de mi propiedad, se encuentran libres de gravamen y los entrego en el estado en que se hallan, autorizando su custodia hasta la cancelación de la obligación o hasta la fecha de remate pactada.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingTop(36).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Width(200).BorderTop(0.5f).BorderColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text(d.ApellidosNombres).Bold().FontSize(8);
                            c.Item().Text($"DNI {d.DniCliente}").FontSize(8);
                            c.Item().Text("Entregué conforme").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Width(200).BorderTop(0.5f).BorderColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text("CREDICONFIABLE").Bold().FontSize(8);
                            c.Item().Text("Recibí conforme").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim();
}
