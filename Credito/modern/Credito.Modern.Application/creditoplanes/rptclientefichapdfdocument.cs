using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Ficha cliente — datos <c>rptCliente.rdlc</c> / layout <see cref="CredixReportPage"/>.
/// </summary>
public static class RptClienteFichaPdfDocument
{
    public static byte[] Build(RptClienteInformeDto informe)
    {
        var f = informe.Ficha;
        var meta = new List<(string, string)>
        {
            ("Persona ID", f.PersonaId.ToString(CreditoPdfBranding.Inv)),
            ("Documento", f.NumeroDocumento),
            ("Cliente", f.Cliente),
            ("Créditos desembolsados", f.CreditosDesembolsados.ToString(CreditoPdfBranding.Inv)),
        };

        return CredixReportPage.Build(
            "FICHA DE CLIENTE",
            meta,
            col =>
            {
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
                        .Text(f.Nota.Trim());
                }

                if (informe.Avales.Count == 0)
                {
                    return;
                }

                col.Item().Element(c =>
                    CreditoPdfBranding.ComposeSectionTitle(c, "AVALES / CRÉDITOS RELACIONADOS"));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(52);
                        columns.ConstantColumn(58);
                        columns.RelativeColumn();
                        columns.ConstantColumn(68);
                        columns.ConstantColumn(52);
                        columns.ConstantColumn(84);
                        columns.ConstantColumn(72);
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
                        CredixPdfCellText.Write(
                            table.Cell().Element(B),
                            a.CreditoId.ToString(CreditoPdfBranding.Inv),
                            CredixReportTokens.FontSizeBody);
                        table.Cell().Element(B).Text(CreditoPdfBranding.OrDash(a.Persona));
                        CredixPdfCellText.Write(
                            table.Cell().Element(B).AlignRight(),
                            CreditoPdfBranding.Money(a.MontoCredito),
                            CredixReportTokens.FontSizeBody);
                        CredixPdfCellText.Write(table.Cell().Element(B), a.Estado, CredixReportTokens.FontSizeBody);
                        CredixPdfCellText.Write(
                            table.Cell().Element(B),
                            CreditoPdfBranding.OrDash(a.Dni),
                            CredixReportTokens.FontSizeBody);
                        CredixPdfCellText.Write(
                            table.Cell().Element(B),
                            CreditoPdfBranding.OrDash(a.Celular),
                            CredixReportTokens.FontSizeBody);
                    }
                });
            },
            new CredixReportPage.Options
            {
                MetaColumns = 2,
                FooterRowCount = informe.Avales.Count,
            });
    }
}
