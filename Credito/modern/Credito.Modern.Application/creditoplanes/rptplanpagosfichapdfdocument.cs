using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Plan de pagos — datos <c>rptPlanPago.rdlc</c> / layout <see cref="CredixReportPage"/>.
/// </summary>
public static class RptPlanPagosFichaPdfDocument
{
    public static byte[] Build(
        RptEstadoCreditoCabeceraDto cab,
        IReadOnlyList<RptPlanPagosRowDto> cuotas)
    {
        var inv = CreditoPdfBranding.Inv;
        var totalAmort = cuotas.Sum(x => x.Amortizacion);
        var totalInt = cuotas.Sum(x => x.Interes);
        var totalGa = cuotas.Sum(x => x.GastosAdm);
        var totalCuota = cuotas.Sum(x => x.Cuota);
        var meta = new List<(string, string)>
        {
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
            ("Desembolsó", CreditoPdfBranding.OrDash(cab.UsuarioDesembolso)),
        };

        return CredixReportPage.Build(
            $"PLAN DE PAGOS — CRÉDITO N° {cab.CreditoId}",
            meta,
            col =>
            {
                col.Item().Element(c =>
                    CreditoPdfBranding.ComposeSectionTitle(c, "CRONOGRAMA DE PAGOS"));

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(0.45f);
                        c.RelativeColumn(1.15f);
                        c.RelativeColumn(1.05f);
                        c.RelativeColumn(1.05f);
                        c.RelativeColumn(0.95f);
                        c.RelativeColumn(0.9f);
                        c.RelativeColumn(1.1f);
                    });

                    table.Header(h =>
                    {
                        void H(string label, bool right = false)
                        {
                            var cell = h.Cell().Element(CreditoPdfBranding.TableHeaderCell);
                            if (right) cell.AlignRight().Text(label);
                            else cell.AlignCenter().Text(label);
                        }

                        H("N°");
                        H("Capital", true);
                        H("Fecha pago");
                        H("Amort.", true);
                        H("Interés", true);
                        H("G. Adm.", true);
                        H("Cuota", true);
                    });

                    var i = 0;
                    foreach (var cu in cuotas)
                    {
                        var zebra = i++ % 2 == 1;
                        IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                        table.Cell().Element(B).AlignCenter().Text(cu.Numero.ToString(inv));
                        table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Capital));
                        table.Cell().Element(B).AlignCenter().Text(CreditoPdfBranding.DateShort(cu.FechaPago));
                        table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Amortizacion));
                        table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Interes));
                        table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.GastosAdm));
                        table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Cuota));
                    }

                    table.Cell().ColumnSpan(3).Element(CreditoPdfBranding.TableTotalCell)
                        .AlignRight().Text("Total").Bold();
                    table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                        .Text(CreditoPdfBranding.Money(totalAmort)).Bold();
                    table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                        .Text(CreditoPdfBranding.Money(totalInt)).Bold();
                    table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                        .Text(CreditoPdfBranding.Money(totalGa)).Bold();
                    table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                        .Text(CreditoPdfBranding.Money(totalCuota)).Bold();
                });
            },
            new CredixReportPage.Options { FooterRowCount = cuotas.Count });
    }

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
            "—",
            0,
            0,
            string.Empty,
            cuotas.Sum(x => x.Cuota),
            0);
        return Build(stub, cuotas);
    }
}
