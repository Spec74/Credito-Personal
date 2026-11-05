using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Carta PDF de notificación de vencimiento para un crédito/cliente
/// (imprimir o enviar). Datos de ficha + estado de cuenta + mora.
/// </summary>
public static class RptNotificacionVencimientoPdfDocument
{
    static RptNotificacionVencimientoPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        RptEstadoCreditoInformeDto estado,
        RptClienteFichaDto? ficha,
        decimal? moraPendiente,
        CreditoVencidoMetricasDto? metricas)
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

        var totalCuotasVencidas = vencidas.Sum(c => c.Cuota);
        var totalMoraCuotas = vencidas.Sum(c => c.ImporteMora ?? 0m);
        var maxDias = vencidas.Count == 0 ? 0 : vencidas.Max(c => c.DiasAtrazo ?? 0);
        var creditoVencido = metricas?.CreditoVencido ?? totalCuotasVencidas;
        var mora = moraPendiente ?? totalMoraCuotas;
        var cliente = ficha?.Cliente ?? cab.Cliente;
        var documento = ficha?.NumeroDocumento ?? cab.CodigoPersona;

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
                        $"{documento} · {cliente}");

                    col.Item()
                        .Background(CreditoPdfBranding.MetaBg)
                        .Border(0.5f)
                        .BorderColor(CreditoPdfBranding.Border)
                        .Padding(8)
                        .Text(
                            "Se notifica el vencimiento de obligaciones del crédito indicado. " +
                            "Documento para entrega o envío al cliente.")
                        .FontSize(8);

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "DATOS DEL CLIENTE"));
                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Cliente", cliente),
                        ("Documento", CreditoPdfBranding.OrDash(documento)),
                        ("Celular", CreditoPdfBranding.OrDash(ficha?.Celular)),
                        ("Distrito", CreditoPdfBranding.OrDash(ficha?.Distrito)),
                        ("Dirección", CreditoPdfBranding.OrDash(ficha?.Direccion)),
                        ("Ref. dirección", CreditoPdfBranding.OrDash(ficha?.DireccionRef)),
                    ], columns: 2));

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "DATOS DEL CRÉDITO"));
                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Crédito N°", cab.CreditoId.ToString(CreditoPdfBranding.Inv)),
                        ("Producto", CreditoPdfBranding.OrDash(cab.Producto)),
                        ("Estado", CreditoPdfBranding.OrDash(cab.Estado)),
                        ("Modalidad", CreditoPdfBranding.OrDash(cab.Modalidad)),
                        ("Monto crédito", CreditoPdfBranding.Money(cab.MontoCredito)),
                        ("Desembolso", CreditoPdfBranding.Money(cab.MontoDesembolso)),
                        ("N° cuotas", cab.NumeroCuotas.ToString(CreditoPdfBranding.Inv)),
                        ("1.er pago", CreditoPdfBranding.DateShort(cab.FechaPrimerPago)),
                        ("Vencimiento", CreditoPdfBranding.DateShort(cab.FechaVencimiento)),
                        ("Analista", CreditoPdfBranding.OrDash(cab.Analista)),
                        ("Cuotas vencidas", vencidas.Count.ToString(CreditoPdfBranding.Inv)),
                        ("Máx. días atraso", maxDias.ToString(CreditoPdfBranding.Inv)),
                        ("Importe vencido", CreditoPdfBranding.Money(creditoVencido)),
                        ("Mora pendiente", CreditoPdfBranding.Money(mora)),
                    ]));

                    col.Item().Element(c =>
                        CreditoPdfBranding.ComposeSectionTitle(c, "CUOTAS VENCIDAS / PENDIENTES DE PAGO"));

                    if (vencidas.Count == 0)
                    {
                        col.Item().PaddingTop(8).AlignCenter()
                            .Text("Este crédito no registra cuotas vencidas a la fecha de emisión.")
                            .FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(28);
                                c.RelativeColumn(1.1f);
                                c.RelativeColumn(1.0f);
                                c.RelativeColumn(0.9f);
                                c.RelativeColumn(0.85f);
                                c.ConstantColumn(40);
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
                                H("Vence");
                                H("Cuota", true);
                                H("Capital", true);
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
                                    .Text(CreditoPdfBranding.Money(cu.Cuota));
                                table.Cell().Element(B).AlignRight()
                                    .Text(CreditoPdfBranding.Money(cu.Amortizacion));
                                table.Cell().Element(B).AlignRight()
                                    .Text(CreditoPdfBranding.Money(cu.ImporteMora));
                                table.Cell().Element(B).AlignCenter()
                                    .Text((cu.DiasAtrazo ?? 0).ToString(CreditoPdfBranding.Inv));
                                table.Cell().Element(B).AlignCenter()
                                    .Text(CreditoPdfBranding.OrDash(cu.Estado));
                            }

                            table.Cell().ColumnSpan(2).Element(CreditoPdfBranding.TableTotalCell)
                                .Text($"Total ({vencidas.Count} cuotas)");
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                                .Text(CreditoPdfBranding.Money(totalCuotasVencidas));
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell);
                            table.Cell().Element(CreditoPdfBranding.TableTotalCell).AlignRight()
                                .Text(CreditoPdfBranding.Money(totalMoraCuotas));
                            table.Cell().ColumnSpan(2).Element(CreditoPdfBranding.TableTotalCell);
                        });
                    }

                    col.Item().PaddingTop(10).Text(t =>
                    {
                        t.Span("Se solicita regularizar el pago a la brevedad en la oficina de ")
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
