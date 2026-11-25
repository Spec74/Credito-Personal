using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Movimientos del crédito — paridad datos <c>usp_RptMovimientoCredito</c> /
/// <c>rptCreditoMov.rdlc</c> con layout estándar Saldos de Caja
/// (banda título, meta SoftBg, tabla azul, firmas, pie).
/// </summary>
public static class RptMovimientoCreditoFichaPdfDocument
{
    static RptMovimientoCreditoFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        RptEstadoCreditoCabeceraDto? cab,
        int creditoId,
        IReadOnlyList<RptMovimientoCreditoRowDto> movimientos)
    {
        var totalImporte = movimientos.Sum(x => x.ImportePago ?? 0m);
        var saldoFinal = movimientos.LastOrDefault()?.Saldo;
        var meta = cab is not null
            ? new List<(string, string)>
            {
                ("Cliente", cab.Cliente),
                ("Producto", cab.Producto),
                ("Estado", cab.Estado),
                ("Monto crédito", CreditoPdfBranding.Money(cab.MontoCredito)),
                ("Desembolso", CreditoPdfBranding.Money(cab.MontoDesembolso)),
                ("Gastos adm.", CreditoPdfBranding.Money(cab.MontoGastosAdm)),
                ("Modalidad", cab.Modalidad),
                ("Cuotas", cab.NumeroCuotas.ToString(CreditoPdfBranding.Inv)),
                ("TEM", $"{cab.Interes.ToString("N2", CreditoPdfBranding.Inv)} %"),
                ("1.er pago", CreditoPdfBranding.DateShort(cab.FechaPrimerPago)),
                ("Vencimiento", CreditoPdfBranding.DateShort(cab.FechaVencimiento)),
                ("Analista", CreditoPdfBranding.OrDash(cab.Analista)),
                ("Desembolsó", CreditoPdfBranding.OrDash(cab.UsuarioDesembolso)),
            }
            : new List<(string, string)>
            {
                ("Crédito", creditoId.ToString(CreditoPdfBranding.Inv)),
                ("Movimientos", movimientos.Count.ToString(CreditoPdfBranding.Inv)),
            };

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(CreditoPdfBranding.PageMarginPt);
                page.DefaultTextStyle(x => x.FontSize(CredixReportTokens.FontSizeBody));

                page.Header().Element(c => CreditoPdfBranding.ComposePageHeader(
                    c,
                    $"MOVIMIENTOS DEL CRÉDITO N° {creditoId}",
                    meta));

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "DETALLE DE MOVIMIENTOS"));

                    if (movimientos.Count == 0)
                    {
                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("No hay movimientos registrados para este crédito.")
                            .FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(72);
                                c.ConstantColumn(88);
                                c.RelativeColumn();
                                c.ConstantColumn(70);
                                c.ConstantColumn(70);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Fecha");
                                h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Operación");
                                h.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Glosa");
                                h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Importe");
                                h.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Saldo");
                            });

                            var i = 0;
                            foreach (var m in movimientos)
                            {
                                var zebra = i++ % 2 == 1;
                                IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                                table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(m.Fecha));
                                table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(m.Operacion));
                                table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(m.Glosa));
                                table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(m.ImportePago));
                                table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(m.Saldo));
                            }

                            table.Cell().ColumnSpan(3).Element(CreditoPdfBranding.TableTotalCell)
                                .Text($"Total movimientos: {movimientos.Count}");
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                                .Text(CreditoPdfBranding.Money(totalImporte));
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                                .Text(saldoFinal is null ? "—" : CreditoPdfBranding.Money(saldoFinal));
                        });
                    }

                    col.Item().PaddingTop(16).Element(CreditoPdfBranding.ComposeFirmas);
                });

                page.Footer().Element(c =>
                    CredixReportTokens.ComposeStandardFooter(c, movimientos.Count));
            });
        }).GeneratePdf();
    }
}
