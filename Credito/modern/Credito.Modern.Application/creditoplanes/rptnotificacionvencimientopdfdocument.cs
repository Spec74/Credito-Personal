using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Carta PDF de notificación de vencimiento para un crédito/cliente.
/// </summary>
public static class RptNotificacionVencimientoPdfDocument
{
    static RptNotificacionVencimientoPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        RptEstadoCreditoInformeDto estado,
        RptClienteFichaDto? ficha)
    {
        var cab = estado.Cabecera;
        var hoy = DateTime.Today;
        var vencidas = estado.Cuotas
            .Where(c =>
            {
                var est = c.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
                if (est == "PAG")
                {
                    return false;
                }

                if ((c.DiasAtrazo ?? 0) > 0)
                {
                    return true;
                }

                return c.FechaVencimiento.Date < hoy;
            })
            .OrderBy(c => c.Numero)
            .ToList();

        // PagoCuota PEN = saldo real (cuota − abonos + mora/cargos). No sumar Cuota+Mora a ciegas.
        var totalPagar = EstadoPlanPagoMontos.TotalSaldoPorPagar(vencidas);
        var totalMora = vencidas.Sum(c => c.ImporteMora ?? 0m);
        var maxDias = vencidas.Count == 0 ? 0 : vencidas.Max(c => c.DiasAtrazo ?? 0);
        var cliente = ficha?.Cliente ?? cab.Cliente;
        var documento = ficha?.NumeroDocumento ?? cab.CodigoPersona;
        var direccion = string.Join(
            " · ",
            new[] { ficha?.Direccion, ficha?.Distrito }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Footer().Element(CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        "NOTIFICACIÓN DE VENCIMIENTO",
                        $"Crédito N° {cab.CreditoId.ToString(CreditoPdfBranding.Inv)}");

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "CLIENTE"));
                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Cliente", cliente),
                        ("Documento", CreditoPdfBranding.OrDash(documento)),
                        ("Celular", CreditoPdfBranding.OrDash(ficha?.Celular)),
                        ("Dirección", CreditoPdfBranding.OrDash(direccion)),
                    ], columns: 2));

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "RESUMEN"));
                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Modalidad", CreditoPdfBranding.OrDash(cab.Modalidad)),
                        ("Cuotas vencidas", vencidas.Count.ToString(CreditoPdfBranding.Inv)),
                        ("Máx. días atraso", maxDias.ToString(CreditoPdfBranding.Inv)),
                        ("Mora", CreditoPdfBranding.Money(totalMora)),
                        ("Total a pagar", CreditoPdfBranding.Money(totalPagar)),
                    ], columns: 2));

                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "CUOTAS VENCIDAS"));

                    if (vencidas.Count == 0)
                    {
                        col.Item().PaddingTop(8).AlignCenter()
                            .Text("No hay cuotas vencidas a la fecha de emisión.")
                            .FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(28);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.1f);
                                c.RelativeColumn(0.95f);
                                c.ConstantColumn(44);
                                c.RelativeColumn(0.9f);
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
                                H("Vence");
                                H("A pagar", true);
                                H("Mora", true);
                                H("Días");
                                H("Estado");
                            });

                            var i = 0;
                            foreach (var cu in vencidas)
                            {
                                var zebra = i++ % 2 == 1;
                                IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                                table.Cell().Element(B).AlignCenter()
                                    .Text(cu.Numero.ToString(CreditoPdfBranding.Inv));
                                table.Cell().Element(B).AlignCenter()
                                    .Text(CreditoPdfBranding.DateShort(cu.FechaVencimiento));
                                table.Cell().Element(B).AlignRight()
                                    .Text(CreditoPdfBranding.Money(EstadoPlanPagoMontos.SaldoPorPagar(cu)));
                                table.Cell().Element(B).AlignRight()
                                    .Text(CreditoPdfBranding.Money(cu.ImporteMora));
                                table.Cell().Element(B).AlignCenter()
                                    .Text((cu.DiasAtrazo ?? 0).ToString(CreditoPdfBranding.Inv));
                                table.Cell().Element(B).AlignCenter()
                                    .Text(CreditoPdfBranding.OrDash(cu.Estado));
                            }

                            table.Cell().ColumnSpan(2).Element(CreditoPdfBranding.TableTotalCell)
                                .Text($"Total ({vencidas.Count})");
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                                .Text(CreditoPdfBranding.Money(totalPagar));
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                                .Text(CreditoPdfBranding.Money(totalMora));
                            table.Cell().ColumnSpan(2).Element(CreditoPdfBranding.TableTotalCell);
                        });
                    }

                    col.Item().PaddingTop(10).Text(t =>
                    {
                        t.Span("Se solicita regularizar el pago en ")
                            .FontSize(8.5f);
                        t.Span(CredixReportTokens.CompanyLegalName).SemiBold().FontSize(8.5f);
                        t.Span(" o por los canales habilitados.").FontSize(8.5f);
                    });

                    col.Item().PaddingTop(28).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderTop(0.6f).BorderColor(CreditoPdfBranding.Border).PaddingTop(4)
                                .AlignCenter().Text("Firma / sello empresa").FontSize(7.5f)
                                .FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(24);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderTop(0.6f).BorderColor(CreditoPdfBranding.Border).PaddingTop(4)
                                .AlignCenter().Text("Recepción del cliente").FontSize(7.5f)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}
