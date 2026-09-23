using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Plan de pagos — paridad datos <c>rptPlanPago.rdlc</c> + cabecera legacy
/// (cliente, producto, TEM, desembolso, modalidad).
/// </summary>
public static class RptPlanPagosFichaPdfDocument
{
    static RptPlanPagosFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        RptEstadoCreditoCabeceraDto cab,
        IReadOnlyList<RptPlanPagosRowDto> cuotas)
    {
        var inv = CreditoPdfBranding.Inv;
        var totalAmort = cuotas.Sum(x => x.Amortizacion);
        var totalInt = cuotas.Sum(x => x.Interes);
        var totalGa = cuotas.Sum(x => x.GastosAdm);
        var totalCuota = cuotas.Sum(x => x.Cuota);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(8));
                page.Footer().Element(CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(7);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        $"PLAN DE PAGOS — CRÉDITO N° {cab.CreditoId}",
                        CredixReportTokens.CompanyLegalName);

                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Cliente", cab.Cliente),
                        ("Producto", cab.Producto),
                        ("Modalidad", cab.Modalidad),
                        ("Monto crédito", CreditoPdfBranding.Money(cab.MontoCredito)),
                        ("Gastos adm.", CreditoPdfBranding.Money(cab.MontoGastosAdm)),
                        ("Desembolso", CreditoPdfBranding.Money(cab.MontoDesembolso)),
                        ("Cuotas", cab.NumeroCuotas.ToString(inv)),
                        ("TEM", $"{cab.Interes.ToString("N2", inv)} %"),
                        ("1.er pago", CreditoPdfBranding.DateShort(cab.FechaPrimerPago)),
                        ("Vencimiento", CreditoPdfBranding.DateShort(cab.FechaVencimiento)),
                        ("Estado", cab.Estado),
                        ("Analista", CreditoPdfBranding.OrDash(cab.Analista)),
                    ]));

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "CRONOGRAMA DE PAGOS"));

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);
                            c.RelativeColumn();
                            c.ConstantColumn(72);
                            c.ConstantColumn(62);
                            c.ConstantColumn(58);
                            c.ConstantColumn(52);
                            c.ConstantColumn(62);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("N°");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Capital");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Fecha pago");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Amort.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Interés");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("G. Adm.");
                            h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Cuota");
                        });

                        var i = 0;
                        foreach (var cu in cuotas)
                        {
                            var zebra = i++ % 2 == 1;
                            IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                            table.Cell().Element(B).Text(cu.Numero.ToString(inv));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Capital));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(cu.FechaPago));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Amortizacion));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Interes));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.GastosAdm));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Cuota));
                        }

                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).Text("Total");
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell);
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell);
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalAmort));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalInt));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalGa));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalCuota));
                    });
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Compatibilidad: solo filas (sin cabecera enriquecida).</summary>
    public static byte[] Build(int creditoId, IReadOnlyList<RptPlanPagosRowDto> cuotas)
    {
        var stub = new RptEstadoCreditoCabeceraDto(
            creditoId,
            0,
            "—",
            DateTime.Today,
            DateTime.Today,
            0,
            "—",
            cuotas.Count,
            0,
            "—",
            string.Empty,
            $"Crédito N° {creditoId}",
            "—",
            0,
            cuotas.Sum(x => x.Cuota),
            0);
        return Build(stub, cuotas);
    }
}
