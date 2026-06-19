using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Plan simulador (paridad <c>Reporte/ReporteSimuladorPlanPagos</c> RDLC).</summary>
public static class RptSimuladorPlanPagosPdfDocument
{
    static RptSimuladorPlanPagosPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RptSimuladorPlanPagosInformeDto informe)
    {
        var cab = informe.Cabecera;
        var inv = CultureInfo.InvariantCulture;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Footer().Element(CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        "PLAN DE PAGOS - SIMULADOR",
                        $"{cab.Producto} | {cab.Cliente}");

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => SummaryCard(c, "Monto solicitado", cab.Monto, Colors.Blue.Darken2));
                        row.RelativeItem().Element(c => SummaryCard(c, "Gastos adm.", cab.GastosAdm, Colors.Grey.Darken2));
                        row.RelativeItem().Element(c => SummaryCard(c, "Desembolso", cab.Desembolso, Colors.Green.Darken2));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => SummaryCard(c, "Intereses totales", cab.InteresesTotales, Colors.Orange.Darken2));
                        row.RelativeItem().Element(c => SummaryCard(c, "Total a devolver", cab.TotalDevolver, Colors.Blue.Darken3));
                        row.RelativeItem().Element(c => SummaryCard(c, "Cuota ref.", cab.CuotaReferencial, Colors.Red.Darken2));
                    });

                    col.Item().Element(c => InfoBlock(c, cab));

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.ConstantColumn(72);
                            c.RelativeColumn();
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("N°");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Fecha");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Capital");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Interés");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("G.Adm");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Cuota");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Saldo");
                        });
                        foreach (var cu in informe.Cuotas)
                        {
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.Numero?.ToString(inv) ?? "");
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.FechaPago?.ToString("dd/MM/yyyy", inv) ?? "");
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.Amortizacion ?? 0).ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.Interes ?? 0).ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.GastosAdm ?? 0).ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.Cuota ?? 0).ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.Saldo ?? 0).ToString("N2", inv));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void SummaryCard(IContainer container, string label, string value, Color color)
    {
        container
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(col =>
            {
                col.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
                col.Item().Text(value).Bold().FontSize(11).FontColor(color);
            });
    }

    private static void InfoBlock(IContainer container, RptSimuladorPlanPagosCabeceraDto cab)
    {
        container
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(col =>
            {
                col.Spacing(3);
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Cliente: ").Bold(); t.Span(cab.Cliente); });
                    r.RelativeItem().Text(t => { t.Span(cab.TipoDocumento + ": ").Bold(); t.Span(cab.NroDocumento); });
                    r.RelativeItem().Text(t => { t.Span("Teléfono: ").Bold(); t.Span(cab.TelefonoCliente); });
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Producto: ").Bold(); t.Span(cab.Producto); });
                    r.RelativeItem().Text(t => { t.Span("Modalidad: ").Bold(); t.Span(cab.Modalidad); });
                    r.RelativeItem().Text(t => { t.Span("Cuotas: ").Bold(); t.Span(cab.Cuotas); });
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("TEM: ").Bold(); t.Span(cab.Tem); });
                    r.RelativeItem().Text(t => { t.Span("Primer pago: ").Bold(); t.Span(cab.Fecha); });
                    r.RelativeItem().Text(t => { t.Span("Último pago: ").Bold(); t.Span(cab.FechaUltimoPago); });
                });
                col.Item().Text(t => { t.Span("Asesor: ").Bold(); t.Span(cab.Asesor); });
                col.Item().Text(t => { t.Span("Dirección domicilio: ").Bold(); t.Span(cab.DireccionCliente); });
                col.Item().Text(t => { t.Span("Dirección negocio: ").Bold(); t.Span(cab.DireccionNegocio); });
                if (!string.Equals(cab.PrendaDescripcion, "Ninguna", StringComparison.OrdinalIgnoreCase))
                {
                    col.Item()
                        .Background(Colors.Yellow.Lighten4)
                        .Padding(5)
                        .Text(t => { t.Span("Garantía / prenda: ").Bold(); t.Span(cab.PrendaDescripcion); });
                }
            });
    }
}
