using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptEstadoCreditoFichaPdfDocument
{
    static RptEstadoCreditoFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RptEstadoCreditoInformeDto informe)
    {
        var cab = informe.Cabecera;
        var inv = CultureInfo.InvariantCulture;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        $"ESTADO DE CRÉDITO {cab.CreditoId}",
                        $"{cab.Cliente} · CREDIX");
                    col.Item().Text(
                        $"{cab.Cliente} · {cab.Producto} · {cab.Estado} · Monto {cab.MontoCredito:N2} · Cuotas {cab.NumeroCuotas}");
                    col.Item().Text(
                        $"Vence {cab.FechaVencimiento:dd/MM/yyyy} · Analista {cab.Analista} · Total {cab.Total:N2}");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.ConstantColumn(52);
                            c.ConstantColumn(62);
                            c.ConstantColumn(48);
                            c.ConstantColumn(48);
                            c.ConstantColumn(42);
                            c.ConstantColumn(48);
                            c.ConstantColumn(40);
                            c.ConstantColumn(48);
                            c.ConstantColumn(40);
                            c.ConstantColumn(48);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("N°");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Capital");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Vence");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Amort.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Int.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("G.A.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Cuota");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Est.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Mora");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Pagado");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Atr.");
                        });
                        foreach (var cu in informe.Cuotas)
                        {
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.Numero.ToString(inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Capital.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.FechaVencimiento.ToString("dd/MM/yy", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Amortizacion.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Interes.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.GastosAdm.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Cuota.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.Estado);
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.ImporteMora ?? 0).ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text((cu.PagoCuota ?? 0).ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.DiasAtrazo?.ToString(inv) ?? "");
                        }
                    });
                });
            });
        }).GeneratePdf();
    }
}
