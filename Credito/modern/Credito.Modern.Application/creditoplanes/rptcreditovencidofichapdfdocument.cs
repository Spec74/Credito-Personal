using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF profesional de créditos vencidos — paridad datos
/// <c>CREDITO.usp_RptCreditoVencido</c> / RDLC.
/// </summary>
public static class RptCreditoVencidoFichaPdfDocument
{
    static RptCreditoVencidoFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptCreditoVencidoRowDto> rows,
        CredixLegacyReportContext? context = null)
    {
        var totalMonto = rows.Sum(x => x.MontoCredito);
        var totalVencido = rows.Sum(x => x.CreditoVencido ?? 0m);
        var oficina = CreditoPdfBranding.OrDash(context?.Oficina);
        var franjas = BuildFranjaLabel(rows);

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
                        "REPORTE CRÉDITOS VENCIDOS",
                        CredixReportTokens.CompanyLegalName);

                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Oficina", oficina == "—" ? "TODOS" : oficina),
                        ("Franjas", franjas),
                        ("N° créditos", rows.Count.ToString(CreditoPdfBranding.Inv)),
                        ("Total monto", CreditoPdfBranding.Money(totalMonto)),
                        ("Total vencido", CreditoPdfBranding.Money(totalVencido)),
                        ("Fecha", context?.Fecha ?? CredixReportTokens.NowPrinted()),
                    ]));

                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "DETALLE DE CRÉDITOS VENCIDOS"));

                    if (rows.Count == 0)
                    {
                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("No hay créditos vencidos para los filtros indicados.")
                            .FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(1.2f);
                            c.ConstantColumn(52);
                            c.RelativeColumn(1.8f);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(48);
                            c.ConstantColumn(36);
                            c.ConstantColumn(36);
                            c.ConstantColumn(36);
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
                            H("Gestor");
                            H("Crédito");
                            H("Cliente");
                            H("F. venc.");
                            H("Forma pago");
                            H("Monto", true);
                            H("Vencido", true);
                            H("<60");
                            H(">60");
                            H("Irr.");
                        });

                        var n = 0;
                        foreach (var r in rows)
                        {
                            var zebra = n % 2 == 1;
                            n++;
                            IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                            table.Cell().Element(B).Text(n.ToString(CreditoPdfBranding.Inv));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Gestor));
                            table.Cell().Element(B).Text(r.CreditoId.ToString(CreditoPdfBranding.Inv));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Cliente));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(r.FechaVencimiento));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.FormaPago));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.MontoCredito));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.CreditoVencido));
                            table.Cell().Element(B).AlignCenter().Text(Flag(r.VencidoMenor60));
                            table.Cell().Element(B).AlignCenter().Text(Flag(r.VencidoMayor60));
                            table.Cell().Element(B).AlignCenter().Text(Flag(r.VencidoIrrecuperable));
                        }

                        table.Cell().ColumnSpan(6).Element(CreditoPdfBranding.TableTotalCell)
                            .Text($"Total ({rows.Count} créditos)");
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalMonto));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalVencido));
                        table.Cell().ColumnSpan(3).Element(CreditoPdfBranding.TableTotalCell);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string Flag(string? value) =>
        string.Equals(value?.Trim(), "S", StringComparison.OrdinalIgnoreCase) ? "S" : "—";

    private static string BuildFranjaLabel(IReadOnlyList<RptCreditoVencidoRowDto> rows)
    {
        var menor = rows.Any(r => string.Equals(r.VencidoMenor60?.Trim(), "S", StringComparison.OrdinalIgnoreCase));
        var mayor = rows.Any(r => string.Equals(r.VencidoMayor60?.Trim(), "S", StringComparison.OrdinalIgnoreCase));
        var irr = rows.Any(r => string.Equals(r.VencidoIrrecuperable?.Trim(), "S", StringComparison.OrdinalIgnoreCase));
        var parts = new List<string>();
        if (menor) parts.Add("<60 días");
        if (mayor) parts.Add(">60 días");
        if (irr) parts.Add("Irrecuperable");
        return parts.Count == 0 ? "Todas" : string.Join(" · ", parts);
    }
}
