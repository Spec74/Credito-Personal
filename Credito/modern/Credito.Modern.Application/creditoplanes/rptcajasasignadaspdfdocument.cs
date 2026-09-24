using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de cajas asignadas: paridad <c>rptCajasAsignadas.rdlc</c> (tabla ancha → A4 horizontal,
/// totales, resumen de cuentas) con formato Credix.
/// </summary>
public static class RptCajasAsignadasPdfDocument
{
    static RptCajasAsignadasPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptCajasAsignadasRowDto> rows,
        CredixLegacyReportContext context,
        string? resumenOficina = null)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CajaSaldosPdfStyle.Pe);
        var pe = CajaSaldosPdfStyle.Pe;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(s => s.FontSize(7.2f));

                page.Header().Column(col =>
                {
                    col.Item().Element(c =>
                        CajaSaldosPdfStyle.TitleBand(c, logo, "CAJAS ASIGNADAS", printedAt));
                    col.Item().PaddingTop(4).Element(c =>
                        CredixLegacyPdfDocument.ComposeMetadataBlock(
                            c,
                            BuildMeta(context, resumenOficina)));
                });

                page.Content().PaddingTop(6).Element(c => ComposeTable(c, rows, pe));
                CajaSaldosPdfStyle.Footer(page, rows.Count);
            });
        }).GeneratePdf();
    }

    private static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> BuildMeta(
        CredixLegacyReportContext context,
        string? resumenOficina)
    {
        var lines = new List<CredixLegacyPdfDocument.MetadataLine>();
        if (!string.IsNullOrWhiteSpace(context.Oficina))
            lines.Add(new CredixLegacyPdfDocument.MetadataLine("Oficina: ", context.Oficina));
        if (!string.IsNullOrWhiteSpace(resumenOficina))
            lines.Add(new CredixLegacyPdfDocument.MetadataLine(
                "Resumen de cuentas: ",
                ResumenCuentaCajaParser.FormatLine(resumenOficina, CajaSaldosPdfStyle.Pe)));
        return lines;
    }

    private static void ComposeTable(
        IContainer container,
        IReadOnlyList<RptCajasAsignadasRowDto> rows,
        System.Globalization.CultureInfo pe)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(42);
                columns.RelativeColumn(1.4f);
                columns.ConstantColumn(58);
                columns.RelativeColumn(1.8f);
                columns.ConstantColumn(78);
                columns.ConstantColumn(78);
                columns.RelativeColumn(0.85f);
                columns.RelativeColumn(0.85f);
                columns.RelativeColumn(0.85f);
                columns.RelativeColumn(0.95f);
                columns.RelativeColumn(1.6f);
            });

            table.Header(h =>
            {
                foreach (var label in new[]
                {
                    "Id", "Caja", "Modo", "Gestor", "Inicio", "Fin",
                    "Saldo inicial", "Entradas", "Salidas", "Saldo final", "Resumen",
                })
                {
                    h.Cell().Element(CajaSaldosPdfStyle.HeaderCell)
                        .AlignCenter()
                        .Text(label).Bold().FontColor(Colors.White).FontSize(6.8f);
                }
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = i % 2 == 0 ? Colors.White : CajaSaldosPdfStyle.Zebra;
                Text(table, r.CajaDiarioId.ToString(System.Globalization.CultureInfo.InvariantCulture), bg, center: true);
                Text(table, r.Caja, bg);
                Text(table, r.Modo, bg, center: true);
                Text(table, r.Cajero, bg);
                Text(table, CajaSaldosPdfStyle.DateTime(r.FechaIniOperacion), bg, center: true);
                Text(table, r.FechaFinOperacion is null ? "—" : CajaSaldosPdfStyle.DateTime(r.FechaFinOperacion.Value), bg, center: true);
                Money(table, r.SaldoInicial, bg);
                Money(table, r.Entradas, bg);
                Money(table, r.Salidas, bg);
                Money(table, r.SaldoFinal, bg, bold: true);
                Text(table, ResumenCuentaCajaParser.FormatLine(r.Resumen, pe), bg);
            }

            if (rows.Count == 0)
            {
                return;
            }

            table.Cell().ColumnSpan(6).Element(CajaSaldosPdfStyle.TotalsCell)
                .AlignRight().Text("TOTAL").Bold();
            Money(table, rows.Sum(r => r.SaldoInicial), CajaSaldosPdfStyle.TotalsBg, bold: true, totals: true);
            Money(table, rows.Sum(r => r.Entradas), CajaSaldosPdfStyle.TotalsBg, bold: true, totals: true);
            Money(table, rows.Sum(r => r.Salidas), CajaSaldosPdfStyle.TotalsBg, bold: true, totals: true);
            Money(table, rows.Sum(r => r.SaldoFinal), CajaSaldosPdfStyle.TotalsBg, bold: true, totals: true);
            table.Cell().Element(CajaSaldosPdfStyle.TotalsCell);
        });
    }

    private static void Text(TableDescriptor table, string? value, Color bg, bool center = false)
    {
        var cell = table.Cell().Element(c => CajaSaldosPdfStyle.BodyCell(c, bg));
        if (center)
            cell = cell.AlignCenter();
        cell.Text(value ?? string.Empty).FontSize(6.8f);
    }

    private static void Money(
        TableDescriptor table,
        decimal value,
        Color bg,
        bool bold = false,
        bool totals = false)
    {
        var cell = table.Cell()
            .Element(c => totals ? CajaSaldosPdfStyle.TotalsCell(c) : CajaSaldosPdfStyle.BodyCell(c, bg))
            .AlignRight();
        var text = cell.Text(CajaSaldosPdfStyle.Money(value)).FontSize(6.8f);
        if (bold)
            text.Bold();
    }
}
