using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Estado de cuenta del crédito — paridad datos <c>rptEstadoCredito.rdlc</c>
/// con layout estándar Saldos de Caja (banda título, meta SoftBg, tabla azul,
/// firmas, pie). Horizontal por densidad de columnas.
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
        var totalPagado = EstadoPlanPagoMontos.TotalPagado(informe.Cuotas);
        var meta = new List<(string, string)>
        {
            ("Cliente", cab.Cliente),
            ("Producto", cab.Producto),
            ("Modalidad", cab.Modalidad),
            ("Estado", cab.Estado),
            ("Cuotas", cab.NumeroCuotas.ToString(inv)),
            ("Monto crédito", CreditoPdfBranding.Money(cab.MontoCredito)),
            ("Gastos adm.", CreditoPdfBranding.Money(cab.MontoGastosAdm)),
            ("Desembolso", CreditoPdfBranding.Money(cab.MontoDesembolso)),
            ("TEM", $"{cab.Interes.ToString("N2", inv)} %"),
            ("1.er pago", CreditoPdfBranding.DateShort(cab.FechaPrimerPago)),
            ("Vencimiento", CreditoPdfBranding.DateShort(cab.FechaVencimiento)),
            ("Analista", CreditoPdfBranding.OrDash(cab.Analista)),
            ("Desembolsó", CreditoPdfBranding.OrDash(cab.UsuarioDesembolso)),
            ("Total crédito", CreditoPdfBranding.Money(cab.Total)),
        };

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontSize(7.2f));

                page.Header().Element(c => CreditoPdfBranding.ComposePageHeader(
                    c,
                    $"ESTADO DE CUENTA — CRÉDITO N° {cab.CreditoId}",
                    meta));

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "CRONOGRAMA / ESTADO DE CUOTAS"));

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(0.45f);
                            c.RelativeColumn(1.05f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(0.95f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(0.75f);
                            c.RelativeColumn(0.95f);
                            c.RelativeColumn(0.55f);
                            c.RelativeColumn(0.5f);
                            c.RelativeColumn(0.85f);
                            c.RelativeColumn(0.85f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.0f);
                        });

                        table.Header(h =>
                        {
                            void H(string t, bool right = false)
                            {
                                var cell = h.Cell().Element(CreditoPdfBranding.TableHeaderCell);
                                if (right) cell.AlignRight().Text(t);
                                else cell.AlignCenter().Text(t);
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

                            table.Cell().Element(B).AlignCenter().Text(cu.Numero.ToString(inv));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Capital));
                            table.Cell().Element(B).AlignCenter().Text(CreditoPdfBranding.DateShort(cu.FechaVencimiento));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Amortizacion));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Interes));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.GastosAdm));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Cuota));
                            table.Cell().Element(B).AlignCenter().Text(cu.Estado);
                            table.Cell().Element(B).AlignRight().Text(cu.DiasAtrazo?.ToString(inv) ?? "—");
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.ImporteMora));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(cu.Descuento));
                            table.Cell().Element(B).AlignCenter().Text(CreditoPdfBranding.DateShort(cu.FechaPagoCuota));
                            table.Cell().Element(B).AlignRight().Text(
                                CreditoPdfBranding.Money(EstadoPlanPagoMontos.ImportePagado(cu)));
                        }

                        void T(string text, bool right = false)
                        {
                            var cell = table.Cell().Element(CreditoPdfBranding.TableTotalCell);
                            if (right) cell.AlignRight().Text(text).Bold();
                            else cell.AlignCenter().Text(text).Bold();
                        }

                        table.Cell().ColumnSpan(3).Element(CreditoPdfBranding.TableTotalCell)
                            .AlignRight().Text("Total").Bold();
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

                    col.Item().PaddingTop(12).Element(CreditoPdfBranding.ComposeFirmas);
                });

                page.Footer().Element(c =>
                    CredixReportTokens.ComposeStandardFooter(c, informe.Cuotas.Count));
            });
        }).GeneratePdf();
    }
}
