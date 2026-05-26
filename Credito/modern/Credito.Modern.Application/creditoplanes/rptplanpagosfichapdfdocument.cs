using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

public static class RptPlanPagosFichaPdfDocument
{
    static RptPlanPagosFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(int creditoId, IReadOnlyList<RptPlanPagosRowDto> cuotas)
    {
        var inv = CultureInfo.InvariantCulture;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Content().Column(col =>
                {
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        $"PLAN DE PAGOS — CRÉDITO {creditoId}",
                        "CREDIX — exportación moderna (datos = legacy)");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(32);
                            c.RelativeColumn();
                            c.ConstantColumn(72);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(52);
                            c.ConstantColumn(58);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("N°");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Capital");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Fecha");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Amort.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Interés");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("G.Adm");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Cuota");
                        });
                        foreach (var cu in cuotas)
                        {
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.Numero.ToString(inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Capital.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(cu.FechaPago.ToString("dd/MM/yyyy", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Amortizacion.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Interes.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.GastosAdm.ToString("N2", inv));
                            table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(cu.Cuota.ToString("N2", inv));
                        }
                    });
                });
            });
        }).GeneratePdf();
    }
}
