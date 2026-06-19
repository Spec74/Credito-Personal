using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Ficha cliente (paridad layout <c>Reporte/ReporteCliente</c> RDLC).</summary>
public static class RptClienteFichaPdfDocument
{
    static RptClienteFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(RptClienteInformeDto informe)
    {
        var f = informe.Ficha;
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
                        "FICHA DE CLIENTE",
                        $"{f.NumeroDocumento} · {f.Cliente}");
                    col.Item().Text($"Persona ID: {f.PersonaId}  ·  Créditos desembolsados: {f.CreditosDesembolsados}");

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Cliente", f.Cliente));
                        row.RelativeItem().Element(c => FieldBlock(c, "Documento", f.NumeroDocumento));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Fecha nac.", f.FechaNacimiento ?? "—"));
                        row.RelativeItem().Element(c => FieldBlock(c, "Sexo", f.Sexo ?? "—"));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Dirección", f.Direccion ?? "—"));
                        row.RelativeItem().Element(c => FieldBlock(c, "Ref. dirección", f.DireccionRef ?? "—"));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Celular", f.Celular ?? "—"));
                        row.RelativeItem().Element(c => FieldBlock(c, "Distrito", f.Distrito));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Estado civil", f.EstadoCivil));
                        row.RelativeItem().Element(c => FieldBlock(c, "Tipo vivienda", f.TipoVivienda));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Actividad económica", f.ActividadEconomica));
                        row.RelativeItem().Element(c => FieldBlock(c, "Cónyuge", f.Conyugue));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "DNI cónyuge", f.ConyugueDni));
                        row.RelativeItem().Element(c => FieldBlock(c, "Cel. cónyuge", f.ConyugueCelular));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => FieldBlock(c, "Dir. negocio", f.DireccionNegocio ?? "—"));
                        row.RelativeItem().Element(c => FieldBlock(c, "Ref. negocio", f.DireccionNegocioRef ?? "—"));
                    });
                    if (!string.IsNullOrWhiteSpace(f.Nota))
                    {
                        col.Item().Element(c => FieldBlock(c, "Nota", f.Nota));
                    }

                    if (informe.Avales.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Avales / créditos relacionados").Bold().FontSize(10);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(55);
                                columns.RelativeColumn();
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(55);
                                columns.ConstantColumn(80);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Grupo");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Crédito");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Persona");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Monto");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Estado");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("DNI");
                            });
                            foreach (var a in informe.Avales)
                            {
                                table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(a.Grupo);
                                table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(a.CreditoId.ToString());
                                table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(a.Persona ?? "—");
                                table.Cell().Element(CreditoPdfBranding.TableBodyCell).AlignRight().Text(a.MontoCredito.ToString("N2"));
                                table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(a.Estado);
                                table.Cell().Element(CreditoPdfBranding.TableBodyCell).Text(a.Dni ?? "—");
                            }
                        });
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void FieldBlock(IContainer container, string label, string value)
    {
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
        {
            c.Item().Text(label).SemiBold().FontSize(8);
            c.Item().Text(value);
        });
    }

    private static IContainer CellHeader(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(8)).Padding(4).Background(Colors.Grey.Lighten3);

    private static IContainer CellBody(IContainer c) => c.Padding(4).BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten3);
}
