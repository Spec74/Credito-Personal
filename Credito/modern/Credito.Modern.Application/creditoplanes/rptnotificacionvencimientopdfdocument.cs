using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Notificación de vencimiento — carta con chrome <see cref="CredixReportPage"/>
/// y firmas empresa/cliente (no Gerente/Gestor).
/// </summary>
public static class RptNotificacionVencimientoPdfDocument
{
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

        var totalPagar = EstadoPlanPagoMontos.TotalSaldoPorPagar(vencidas);
        var totalMora = vencidas.Sum(c => c.ImporteMora ?? 0m);
        var maxDias = vencidas.Count == 0 ? 0 : vencidas.Max(c => c.DiasAtrazo ?? 0);
        var cliente = ficha?.Cliente ?? cab.Cliente;
        var documento = ficha?.NumeroDocumento ?? cab.CodigoPersona;
        var direccion = string.Join(
            " · ",
            new[] { ficha?.Direccion, ficha?.Distrito }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var meta = new List<(string, string)>
        {
            ("Cliente", cliente),
            ("Documento", CreditoPdfBranding.OrDash(documento)),
            ("Celular", CreditoPdfBranding.OrDash(ficha?.Celular)),
            ("Dirección", CreditoPdfBranding.OrDash(direccion)),
            ("Crédito", cab.CreditoId.ToString(CreditoPdfBranding.Inv)),
            ("Modalidad", CreditoPdfBranding.OrDash(cab.Modalidad)),
            ("Cuotas vencidas", vencidas.Count.ToString(CreditoPdfBranding.Inv)),
            ("Máx. días atraso", maxDias.ToString(CreditoPdfBranding.Inv)),
            ("Mora", CreditoPdfBranding.Money(totalMora)),
            ("Total a pagar", CreditoPdfBranding.Money(totalPagar)),
        };

        return CredixReportPage.Build(
            "NOTIFICACIÓN DE VENCIMIENTO",
            meta,
            col =>
            {
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
                    t.Span("Se solicita regularizar el pago en ").FontSize(8.5f);
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
            },
            new CredixReportPage.Options
            {
                FontSize = 9f,
                Signatures = false,
                FooterRowCount = vencidas.Count,
            });
    }
}
