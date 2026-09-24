using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Estado de cuenta del crédito — paridad datos <c>rptEstadoCredito.rdlc</c>
/// con layout profesional (cabecera, metadatos, cronograma, totales).
/// </summary>
public static class RptEstadoCreditoFichaPdfDocument
{
    static RptEstadoCreditoFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RptEstadoCreditoInformeDto informe)
    {
        var cab = informe.Cabecera;
        var inv = CreditoPdfBranding.Inv;
        var totalAmort = informe.Cuotas.Sum(x => x.Amortizacion);
        var totalInt = informe.Cuotas.Sum(x => x.Interes);
        var totalGa = informe.Cuotas.Sum(x => x.GastosAdm);
        var totalCuota = informe.Cuotas.Sum(x => x.Cuota);
        var totalMora = informe.Cuotas.Sum(x => x.ImporteMora ?? 0m);
        var totalDesc = informe.Cuotas.Sum(x => x.Descuento ?? 0m);
        var totalPagado = informe.Cuotas.Sum(x => x.PagoCuota ?? 0m);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7.5f));
                page.Footer().Element(CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        $"ESTADO DE CUENTA — CRÉDITO N° {cab.CreditoId}",
                        CredixReportTokens.CompanyLegalName);

                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Cliente", cab.Cliente),
                        ("Código", CreditoPdfBranding.OrDash(cab.CodigoPersona)),
                        ("Producto", cab.Producto),
                        ("Estado", cab.Estado),
                        ("Modalidad", cab.Modalidad),
                        ("Cuotas", cab.NumeroCuotas.ToString(inv)),
                        ("Monto crédito", CreditoPdfBranding.Money(cab.MontoCredito)),
                        ("Gastos adm.", CreditoPdfBranding.Money(cab.MontoGastosAdm)),
                        ("Desembolso", CreditoPdfBranding.Money(cab.MontoDesembolso)),
                        ("TEM", $"{cab.Interes.ToString("N2", inv)} %"),
                        ("1.er pago", CreditoPdfBranding.DateShort(cab.FechaPrimerPago)),
                        ("Vencimiento", CreditoPdfBranding.DateShort(cab.FechaVencimiento)),
                        ("Analista", CreditoPdfBranding.OrDash(cab.Analista)),
                        ("Total crédito", CreditoPdfBranding.Money(cab.Total)),
                    ]));

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "CRONOGRAMA / ESTADO DE CUOTAS"));

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.ConstantColumn(48);
                            c.ConstantColumn(52);
                            c.ConstantColumn(42);
                            c.ConstantColumn(42);
                            c.ConstantColumn(36);
                            c.ConstantColumn(46);
                            c.ConstantColumn(32);
                            c.ConstantColumn(28);
                            c.ConstantColumn(42);
                            c.ConstantColumn(40);
                            c.ConstantColumn(52);
                            c.ConstantColumn(48);
                        });

                        table.Header(h =>
                        {
                            void H(string t, bool right = false)
                            {
                                var cell = h.Cell().Element(CreditoPdfBranding.TableHeaderCell);
                                if (right) cell.AlignRight().Text(t);
                                else cell.Text(t);
                            }

                            H("N°");
                            H("Capital", true);
                            H("Vence");
                            H("Amort.", true);
                            H("Int.", true);
                            H("G.A.", true);
                            H("Cuota", true);
                            H("Est.");
                            H("Días", true);
                            H("Mora", true);
                            H("Desc.", true);
                            H("F. pago");
                            H("Pagado", true);
                        });

                        var i = 0;
                        foreach (var cu in informe.Cuotas)
                        {
                            var zebra = i++ % 2 == 1;
                            IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);

                            table.Cell().Element(B).Text(cu.Numero.ToString(inv));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Capital));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(cu.FechaVencimiento));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Amortizacion));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Interes));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.GastosAdm));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Cuota));
                            table.Cell().Element(B).Text(cu.Estado);
                            table.Cell().Element(B).AlignRight().Text(cu.DiasAtrazo?.ToString(inv) ?? "");
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.ImporteMora));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Descuento));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(cu.FechaPagoCuota));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.PagoCuota));
                        }

                        void T(string text, bool right = false)
                        {
                            var cell = table.Cell().Element(CreditoPdfBranding.TableTotalCell);
                            if (right) cell.AlignRight().Text(text);
                            else cell.Text(text);
                        }

                        T("Total");
                        T("", true);
                        T("");
                        T(CreditoPdfBranding.Money(totalAmort), true);
                        T(CreditoPdfBranding.Money(totalInt), true);
                        T(CreditoPdfBranding.Money(totalGa), true);
                        T(CreditoPdfBranding.Money(totalCuota), true);
                        T("");
                        T("");
                        T(CreditoPdfBranding.Money(totalMora), true);
                        T(CreditoPdfBranding.Money(totalDesc), true);
                        T("");
                        T(CreditoPdfBranding.Money(totalPagado), true);
                    });
                });
            });
        }).GeneratePdf();
    }
}
