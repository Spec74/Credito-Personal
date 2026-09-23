using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF profesional de créditos observados — paridad datos
/// <c>CreditoBL.ReporteCreditoObservado</c> / <c>rptCreditoObservado.rdlc</c>.
/// </summary>
public static class RptCreditoObservadoFichaPdfDocument
{
    static RptCreditoObservadoFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptCreditoObservadoRowDto> rows,
        CredixLegacyReportContext? context = null)
    {
        var totalMonto = rows.Sum(x => x.MontoCredito);
        var totalInteres = rows.Sum(x => x.Interes);
        var totalTramite = rows.Sum(x => x.TramiteAdm);
        var totalRiesgo = rows.Sum(x => x.CentralRiesgo);
        var oficina = CreditoPdfBranding.OrDash(context?.Oficina);
        var agente = CreditoPdfBranding.OrDash(context?.Agente ?? context?.Gestor);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontSize(7.5f));
                page.Footer().Element(CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        "REPORTE CRÉDITOS OBSERVADOS",
                        CredixReportTokens.CompanyLegalName);

                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Oficina", oficina == "—" ? "TODOS" : oficina),
                        ("Agente / Gestor", agente == "—" ? "TODOS" : agente),
                        ("N° créditos", rows.Count.ToString(CreditoPdfBranding.Inv)),
                        ("Total monto", CreditoPdfBranding.Money(totalMonto)),
                        ("Total interés", CreditoPdfBranding.Money(totalInteres)),
                        ("Total trámites", CreditoPdfBranding.Money(totalTramite)),
                    ]));

                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "DETALLE DE CRÉDITOS OBSERVADOS"));

                    if (rows.Count == 0)
                    {
                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("No hay créditos observados para los filtros indicados.")
                            .FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(1.1f);
                            c.ConstantColumn(48);
                            c.RelativeColumn(1.6f);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(52);
                            c.ConstantColumn(42);
                            c.ConstantColumn(42);
                            c.ConstantColumn(42);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.4f);
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
                            H("Oficina");
                            H("Crédito");
                            H("Cliente");
                            H("F. inicio");
                            H("F. venc.");
                            H("Monto", true);
                            H("Interés", true);
                            H("Trám.", true);
                            H("C.Riesgo", true);
                            H("Agente");
                            H("Observación");
                        });

                        var n = 0;
                        foreach (var r in rows)
                        {
                            var zebra = n % 2 == 1;
                            n++;
                            IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                            table.Cell().Element(B).Text(n.ToString(CreditoPdfBranding.Inv));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Oficina));
                            table.Cell().Element(B).Text(r.CreditoId.ToString(CreditoPdfBranding.Inv));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Cliente));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(r.FechaPrimerPago));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(r.FechaVencimiento));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.MontoCredito));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.Interes));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.TramiteAdm));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.CentralRiesgo));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Agente));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Observacion));
                        }

                        table.Cell().ColumnSpan(6).Element(CreditoPdfBranding.TableTotalCell)
                            .Text($"Total ({rows.Count} créditos)");
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalMonto));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalInteres));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalTramite));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalRiesgo));
                        table.Cell().ColumnSpan(2).Element(CreditoPdfBranding.TableTotalCell);
                    });
                });
            });
        }).GeneratePdf();
    }
}
