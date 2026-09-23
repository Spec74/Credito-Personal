using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Ficha cliente — paridad <c>rptCliente.rdlc</c> (datos personales + avales)
/// con layout de formulario profesional.
/// </summary>
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
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8.5f));
                page.Footer().Element(CreditoPdfBranding.ComposeFooter);

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    CreditoPdfBranding.ComposeTitleBlock(
                        col,
                        "FICHA DE CLIENTE",
                        $"{f.NumeroDocumento} · {f.Cliente}");

                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Persona ID", f.PersonaId.ToString(CreditoPdfBranding.Inv)),
                        ("Créditos desembolsados", f.CreditosDesembolsados.ToString(CreditoPdfBranding.Inv)),
                        ("Documento", f.NumeroDocumento),
                        ("Cliente", f.Cliente),
                    ], columns: 2));

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "DATOS PERSONALES"));
                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Fecha nacimiento", CreditoPdfBranding.OrDash(f.FechaNacimiento)),
                        ("Sexo", CreditoPdfBranding.OrDash(f.Sexo)),
                        ("Estado civil", CreditoPdfBranding.OrDash(f.EstadoCivil)),
                        ("Tipo vivienda", CreditoPdfBranding.OrDash(f.TipoVivienda)),
                        ("Distrito", CreditoPdfBranding.OrDash(f.Distrito)),
                        ("Celular", CreditoPdfBranding.OrDash(f.Celular)),
                        ("Dirección", CreditoPdfBranding.OrDash(f.Direccion)),
                        ("Ref. dirección", CreditoPdfBranding.OrDash(f.DireccionRef)),
                        ("Actividad económica", CreditoPdfBranding.OrDash(f.ActividadEconomica)),
                        ("Dir. negocio", CreditoPdfBranding.OrDash(f.DireccionNegocio)),
                        ("Ref. negocio", CreditoPdfBranding.OrDash(f.DireccionNegocioRef)),
                    ], columns: 2));

                    col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "CÓNYUGE"));
                    col.Item().Element(c => CreditoPdfBranding.ComposeMetaGrid(c,
                    [
                        ("Nombre", CreditoPdfBranding.OrDash(f.Conyugue)),
                        ("DNI", CreditoPdfBranding.OrDash(f.ConyugueDni)),
                        ("Celular", CreditoPdfBranding.OrDash(f.ConyugueCelular)),
                    ], columns: 2));

                    if (!string.IsNullOrWhiteSpace(f.Nota))
                    {
                        col.Item().Element(c => CreditoPdfBranding.ComposeSectionTitle(c, "NOTA"));
                        col.Item()
                            .Background(CreditoPdfBranding.MetaBg)
                            .Border(0.5f)
                            .BorderColor(CreditoPdfBranding.Border)
                            .Padding(6)
                            .Text(f.Nota);
                    }

                    if (informe.Avales.Count > 0)
                    {
                        col.Item().Element(c =>
                            CreditoPdfBranding.ComposeSectionTitle(c, "AVALES / CRÉDITOS RELACIONADOS"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(48);
                                columns.ConstantColumn(52);
                                columns.RelativeColumn();
                                columns.ConstantColumn(62);
                                columns.ConstantColumn(48);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(62);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Grupo");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Crédito");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Persona");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).AlignRight().Text("Monto");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Estado");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("DNI");
                                header.Cell().Element(CreditoPdfBranding.TableHeaderCell).Text("Celular");
                            });
                            var i = 0;
                            foreach (var a in informe.Avales)
                            {
                                var zebra = i++ % 2 == 1;
                                IContainer B(IContainer x) => CreditoPdfBranding.TableBodyCell(x, zebra);
                                table.Cell().Element(B).Text(a.Grupo);
                                table.Cell().Element(B).Text(a.CreditoId.ToString(CreditoPdfBranding.Inv));
                                table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(a.Persona));
                                table.Cell().Element(B).AlignRight().Text(CreditoPdfBranding.Money(a.MontoCredito));
                                table.Cell().Element(B).Text(a.Estado);
                                table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(a.Dni));
                                table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(a.Celular));
                            }
                        });
                    }
                });
            });
        }).GeneratePdf();
    }
}
