using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF profesional de créditos observados — paridad datos
/// <c>CreditoBL.ReporteCreditoObservado</c> / <c>rptCreditoObservado.rdlc</c>.
/// Hoja horizontal: muchas columnas de texto (oficina, cliente, observación).
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
        const int colCount = 12;
        var bodyFont = CredixLegacyPdfDocument.ResolveBodyFont(colCount);
        var (pageW, pageH) = CredixLegacyPdfDocument.ResolvePageSize(colCount, landscape: true);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(new PageSize(pageW, pageH));
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(bodyFont));
                page.Footer().Element(c => CredixReportTokens.ComposeStandardFooter(c, rows.Count));

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
                            c.RelativeColumn(1.35f);
                            c.ConstantColumn(48);
                            c.RelativeColumn(2.0f);
                            c.ConstantColumn(62);
                            c.ConstantColumn(62);
                            c.ConstantColumn(56);
                            c.ConstantColumn(48);
                            c.ConstantColumn(48);
                            c.ConstantColumn(48);
                            c.RelativeColumn(1.45f);
                            c.RelativeColumn(1.8f);
                        });

                        table.Header(h =>
                        {
                            void H(string t, bool right = false)
                            {
                                var cell = h.Cell().Element(
                                    right
                                        ? CreditoPdfBranding.TableHeaderCell
                                        : CreditoPdfBranding.TableHeaderCell);
                                CredixPdfCellText.Write(
                                    right ? cell.AlignRight() : cell,
                                    t,
                                    bodyFont,
                                    bold: true,
                                    color: Colors.White);
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
                            void Cell(string? text, bool right = false)
                            {
                                var c = table.Cell().Element(B);
                                CredixPdfCellText.Write(
                                    right ? c.AlignRight() : c,
                                    text ?? string.Empty,
                                    bodyFont);
                            }

                            Cell(n.ToString(CreditoPdfBranding.Inv));
                            Cell(CreditoPdfBranding.OrDash(r.Oficina));
                            Cell(r.CreditoId.ToString(CreditoPdfBranding.Inv));
                            Cell(CreditoPdfBranding.OrDash(r.Cliente));
                            Cell(CreditoPdfBranding.DateShort(r.FechaPrimerPago));
                            Cell(CreditoPdfBranding.DateShort(r.FechaVencimiento));
                            Cell(CreditoPdfBranding.Money(r.MontoCredito), right: true);
                            Cell(CreditoPdfBranding.Money(r.Interes), right: true);
                            Cell(CreditoPdfBranding.Money(r.TramiteAdm), right: true);
                            Cell(CreditoPdfBranding.Money(r.CentralRiesgo), right: true);
                            Cell(CreditoPdfBranding.OrDash(r.Agente));
                            Cell(CreditoPdfBranding.OrDash(r.Observacion));
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
