using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF tabular (QuestPDF) con los mismos datos que JSON/CSV; no replica layout <c>rptCreditoObservado.rdlc</c>.
/// </summary>
public static class RptCreditoObservadoPdfFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    static RptCreditoObservadoPdfFormatter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] ToPdf(IReadOnlyList<RptCreditoObservadoRowDto> rows)
    {
        var inv = CultureInfo.InvariantCulture;
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(style => style.FontSize(7));
                page.Header()
                    .Text("Créditos observados")
                    .Bold()
                    .FontSize(11);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(32);
                        columns.RelativeColumn(1.1f);
                        columns.ConstantColumn(36);
                        columns.RelativeColumn(1.4f);
                        columns.ConstantColumn(52);
                        columns.ConstantColumn(52);
                        columns.ConstantColumn(44);
                        columns.ConstantColumn(36);
                        columns.ConstantColumn(32);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.2f);
                        columns.ConstantColumn(40);
                        columns.ConstantColumn(40);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("OficinaId").Bold();
                        header.Cell().Text("Oficina").Bold();
                        header.Cell().Text("CreditoId").Bold();
                        header.Cell().Text("Cliente").Bold();
                        header.Cell().Text("F.PrimerPago").Bold();
                        header.Cell().Text("F.Vencimiento").Bold();
                        header.Cell().Text("Monto").Bold();
                        header.Cell().Text("Interes").Bold();
                        header.Cell().Text("AgenteId").Bold();
                        header.Cell().Text("Agente").Bold();
                        header.Cell().Text("Observacion").Bold();
                        header.Cell().Text("Tramite").Bold();
                        header.Cell().Text("C.Riesgo").Bold();
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Text(r.OficinaId.ToString(inv));
                        table.Cell().Text(r.Oficina ?? string.Empty);
                        table.Cell().Text(r.CreditoId.ToString(inv));
                        table.Cell().Text(r.Cliente ?? string.Empty);
                        table.Cell().Text(r.FechaPrimerPago.ToString(DateTimeFormat, inv));
                        table.Cell().Text(r.FechaVencimiento.ToString(DateTimeFormat, inv));
                        table.Cell().Text(r.MontoCredito.ToString("N2", inv));
                        table.Cell().Text(r.Interes.ToString("N2", inv));
                        table.Cell().Text(r.AgenteId.ToString(inv));
                        table.Cell().Text(r.Agente ?? string.Empty);
                        table.Cell().Text(r.Observacion ?? string.Empty);
                        table.Cell().Text(r.TramiteAdm.ToString("N2", inv));
                        table.Cell().Text(r.CentralRiesgo.ToString("N2", inv));
                    }
                });
                page.Footer()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.Span("Filas: ");
                        text.Span(rows.Count.ToString(inv)).Bold();
                    });
            });
        }).GeneratePdf();
    }
}
