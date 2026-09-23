using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF profesional de clientes inactivos — paridad datos
/// <c>ClienteBL.ReporteClientesInactivos</c> / RDLC.
/// </summary>
public static class RptClientesInactivosFichaPdfDocument
{
    static RptClientesInactivosFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptClientesInactivosRowDto> rows,
        CredixLegacyReportContext? context = null)
    {
        var totalMonto = rows.Sum(x => x.MontoCredito);
        var totalTope = rows.Sum(x => x.TopeCredito);
        var totalCreditos = rows.Sum(x => x.TotalCreditos);
        var oficina = CreditoPdfBranding.OrDash(context?.Oficina);
        var agente = CreditoPdfBranding.OrDash(context?.Agente ?? context?.Gestor);
        var periodo = FormatPeriodo(context);

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
                        "REPORTE CLIENTES INACTIVOS",
                        CredixReportTokens.CompanyLegalName);

                    var meta = new List<(string, string)>
                    {
                        ("Oficina", oficina == "—" ? "TODOS" : oficina),
                        ("Agente / Gestor", agente == "—" ? "TODOS" : agente),
                        ("N° clientes", rows.Count.ToString(CreditoPdfBranding.Inv)),
                        ("Total créditos", totalCreditos.ToString(CreditoPdfBranding.Inv)),
                        ("Suma montos", CreditoPdfBranding.Money(totalMonto)),
                        ("Suma topes", CreditoPdfBranding.Money(totalTope)),
                    };
                    if (!string.IsNullOrWhiteSpace(periodo))
                    {
                        meta.Insert(2, ("Periodo", periodo));
                    }

                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c, meta));

                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "DETALLE DE CLIENTES INACTIVOS"));

                    if (rows.Count == 0)
                    {
                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("No hay clientes inactivos para los filtros indicados.")
                            .FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(1.1f);
                            c.ConstantColumn(52);
                            c.RelativeColumn(1.5f);
                            c.ConstantColumn(48);
                            c.ConstantColumn(52);
                            c.ConstantColumn(36);
                            c.ConstantColumn(48);
                            c.ConstantColumn(48);
                            c.ConstantColumn(40);
                            c.RelativeColumn(1.1f);
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
                            H("Agente");
                            H("Código");
                            H("Cliente");
                            H("DNI");
                            H("Celular");
                            H("Calif.");
                            H("Monto", true);
                            H("Tope", true);
                            H("Créd.", true);
                            H("F. cancel.");
                            H("Días", true);
                        });

                        var n = 0;
                        foreach (var r in rows)
                        {
                            var zebra = n % 2 == 1;
                            n++;
                            IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                            table.Cell().Element(B).Text(n.ToString(CreditoPdfBranding.Inv));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Agente));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Codigo));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Cliente));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Dni));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Celular));
                            table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(r.Calificacion));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.MontoCredito));
                            table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(r.TopeCredito));
                            table.Cell().Element(B).AlignRight().Text(r.TotalCreditos.ToString(CreditoPdfBranding.Inv));
                            table.Cell().Element(B).Text(CreditoPdfBranding.DateShort(r.FechaCancelacion));
                            table.Cell().Element(B).AlignRight().Text(r.DiasInactividad.ToString(CreditoPdfBranding.Inv));
                        }

                        table.Cell().ColumnSpan(7).Element(CreditoPdfBranding.TableTotalCell)
                            .Text($"Total ({rows.Count} clientes)");
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalMonto));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(CreditoPdfBranding.Money(totalTope));
                        table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                            .Text(totalCreditos.ToString(CreditoPdfBranding.Inv));
                        table.Cell().ColumnSpan(2).Element(CreditoPdfBranding.TableTotalCell);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string? FormatPeriodo(CredixLegacyReportContext? context)
    {
        if (context is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(context.FechaIni) || !string.IsNullOrWhiteSpace(context.FechaFin))
        {
            var ini = string.IsNullOrWhiteSpace(context.FechaIni) ? "—" : context.FechaIni.Trim();
            var fin = string.IsNullOrWhiteSpace(context.FechaFin) ? "—" : context.FechaFin.Trim();
            return $"{ini} – {fin}";
        }

        return null;
    }
}
