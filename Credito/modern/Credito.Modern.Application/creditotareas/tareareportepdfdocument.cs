using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoTareas;

/// <summary>
/// PDF de tareas agrupado por cliente — chrome <see cref="CredixReportPage"/> +
/// celdas de tabla vía tokens (sin colores locales).
/// </summary>
public static class TareaReportePdfDocument
{
    private static readonly Color HeaderBg = CredixReportTokens.TableHeaderStrong;
    private static readonly Color GroupBg = CredixReportTokens.SoftBg;
    private static readonly Color BorderColor = CredixReportTokens.BorderSoft;
    private static readonly Color CompletadaBg = CredixReportTokens.SuccessSoft;

    static TareaReportePdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(
        IReadOnlyList<TareaReporteClienteGrupo> grupos,
        string? estado,
        int totalTareas)
    {
        var titulo = TareaReporteBuilder.TituloPdf(estado);
        var inv = CredixReportTokens.Inv;
        var meta = new List<(string, string)>
        {
            ("Total de tareas", totalTareas.ToString(inv)),
            ("Grupos (clientes)", grupos.Count.ToString(inv)),
            ("Filtro estado", string.IsNullOrWhiteSpace(estado) ? "TODOS" : estado.Trim()),
        };

        return CredixReportPage.Build(
            titulo,
            meta,
            col =>
            {
                if (grupos.Count == 0)
                {
                    col.Item().Text("No hay tareas para el filtro seleccionado.").Italic();
                    return;
                }

                foreach (var grupo in grupos)
                {
                    col.Item().PaddingTop(6).Element(c => ComposeGrupo(c, grupo));
                }
            },
            new CredixReportPage.Options
            {
                FontSize = 8f,
                Signatures = false,
                FooterRowCount = totalTareas,
            });
    }

    private static void ComposeGrupo(IContainer container, TareaReporteClienteGrupo grupo)
    {
        var inv = CredixReportTokens.Inv;
        container.Column(col =>
        {
            col.Item()
                .Background(GroupBg)
                .Border(0.5f)
                .BorderColor(BorderColor)
                .Padding(6)
                .Text(grupo.ClienteEtiqueta)
                .Bold()
                .FontColor(CredixReportTokens.BrandDark);

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(52);
                    columns.RelativeColumn(1.2f);
                    columns.ConstantColumn(48);
                    columns.RelativeColumn(2.5f);
                    columns.ConstantColumn(42);
                });

                table.Header(header =>
                {
                    foreach (var h in new[] { "#", "Crédito", "Analista", "Subt.", "Detalle subtareas", "Est." })
                    {
                        header.Cell()
                            .Background(HeaderBg)
                            .Border(0.5f)
                            .BorderColor(HeaderBg)
                            .Padding(3)
                            .AlignCenter()
                            .Text(h)
                            .Bold()
                            .FontColor(Colors.White)
                            .FontSize(7f);
                    }
                });

                foreach (var t in grupo.Tareas)
                {
                    var bg = t.Estado == "COM" ? CompletadaBg : Colors.White;
                    table.Cell().Element(c => BodyCell(c, bg)).Text(t.Nro.ToString(inv)).FontSize(7f);
                    table.Cell().Element(c => BodyCell(c, bg)).Text(t.CreditoId.ToString(inv)).FontSize(7f);
                    table.Cell().Element(c => BodyCell(c, bg)).Text(t.Analista).FontSize(7f);
                    table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text(t.SubtareasResumen).FontSize(7f);
                    table.Cell().Element(c => BodyCell(c, bg)).Text(t.DetalleSubtareas).FontSize(6.5f);
                    table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text(t.Estado).FontSize(7f);
                }
            });
        });
    }

    private static IContainer BodyCell(IContainer container, Color bg) =>
        container
            .Background(bg)
            .BorderLeft(0.4f)
            .BorderRight(0.4f)
            .BorderBottom(0.4f)
            .BorderColor(BorderColor)
            .Padding(3);
}
