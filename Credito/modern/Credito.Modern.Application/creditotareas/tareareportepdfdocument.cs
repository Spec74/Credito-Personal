using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoTareas;

/// <summary>
/// PDF de tareas agrupado por nombre de cliente (paridad operativa legacy + orden alfabético).
/// </summary>
public static class TareaReportePdfDocument
{
    private static readonly Color HeaderBg = Color.FromHex("#2196F3");
    private static readonly Color GroupBg = Color.FromHex("#E3F2FD");
    private static readonly Color BorderColor = Color.FromHex("#B0BEC5");
    private static readonly Color CompletadaBg = Color.FromHex("#E8F5E9");

    static TareaReportePdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(
        IReadOnlyList<TareaReporteClienteGrupo> grupos,
        string? estado,
        int totalTareas)
    {
        var logo = CredixReportAssets.LoadLogo();
        var titulo = TareaReporteBuilder.TituloPdf(estado);
        var fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        var inv = CultureInfo.InvariantCulture;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(s => s.FontSize(8f));

                page.Header().Column(col =>
                {
                    col.Item().Element(c => CredixLegacyPdfDocument.ComposeTitleBand(c, logo, titulo, fecha));
                    col.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Total de tareas: ").Bold();
                        t.Span(totalTareas.ToString(inv));
                    });
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    foreach (var grupo in grupos)
                    {
                        col.Item().PaddingTop(6).Element(c => ComposeGrupo(c, grupo));
                    }

                    if (grupos.Count == 0)
                    {
                        col.Item().Text("No hay tareas para el filtro seleccionado.").Italic();
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeGrupo(IContainer container, TareaReporteClienteGrupo grupo)
    {
        var inv = CultureInfo.InvariantCulture;
        container.Column(col =>
        {
            col.Item()
                .Background(GroupBg)
                .Border(0.5f)
                .BorderColor(BorderColor)
                .Padding(6)
                .Text(grupo.ClienteEtiqueta)
                .Bold()
                .FontColor(Color.FromHex("#1E4D7B"));

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
                            .BorderColor(BorderColor)
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
            .Border(0.25f)
            .BorderColor(BorderColor)
            .Padding(3);
}
