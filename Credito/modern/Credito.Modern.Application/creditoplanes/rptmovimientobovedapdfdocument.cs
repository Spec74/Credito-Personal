using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de movimiento de bóveda: paridad <c>rptMovimientoBoveda.rdlc</c> (cabecera de saldos
/// y estado, detalle de movimientos).
/// </summary>
public static class RptMovimientoBovedaPdfDocument
{
    static RptMovimientoBovedaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptMovimientoBovedaRowDto> rows,
        CredixLegacyReportContext context,
        BovedaAbiertaDto? cab,
        string? resumenCuenta = null)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CajaSaldosPdfStyle.Pe);
        var composicion = ResumenCuentaCajaParser.Compose(resumenCuenta);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(s => s.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Element(c =>
                        CajaSaldosPdfStyle.TitleBand(c, logo, "MOVIMIENTO BÓVEDA", printedAt));
                    col.Item().PaddingTop(6).Element(c => ComposeCab(c, context, cab, composicion));
                });

                page.Content().PaddingTop(8).Element(c => ComposeTable(c, rows));
                CajaSaldosPdfStyle.Footer(page, rows.Count);
            });
        }).GeneratePdf();
    }

    private static void ComposeCab(
        IContainer container,
        CredixLegacyReportContext context,
        BovedaAbiertaDto? cab,
        ResumenCuentaCajaParser.Composicion composicion)
    {
        container.Background(CajaSaldosPdfStyle.BandBg)
            .Border(0.6f)
            .BorderColor(CajaSaldosPdfStyle.Border)
            .Padding(8)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Oficina: ").Bold();
                        t.Span(context.Oficina ?? "—");
                    });
                    row.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Bóveda: ").Bold();
                        t.Span(context.Referencia ?? (cab is null ? "—" : $"N° {cab.BovedaId}"));
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Estado: ").Bold();
                        t.Span(cab is null ? "—" : cab.IndCierre ? "CERRADO" : "ABIERTO");
                    });
                });

                if (cab is not null)
                {
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        Kpi(row, "Saldo inicial", cab.SaldoInicial);
                        Kpi(row, "Entradas", cab.Entradas);
                        Kpi(row, "Salidas", cab.Salidas);
                        Kpi(row, "Saldo final", cab.SaldoFinal, bold: true);
                    });
                }

                // Cabecera (no pie): el cajero ve la composición antes del detalle, como en
                // el resumen de cuentas de la pantalla de bóveda.
                if (composicion.Items.Count > 0)
                {
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        Kpi(row, "Total efectivo", composicion.Efectivo, bold: true);
                        Kpi(row, "Total medios digitales", composicion.MediosDigitales, bold: true);
                    });

                    var detalle = string.Join(
                        " · ",
                        composicion.Items.Select(i =>
                            $"{i.Cuenta} {CajaSaldosPdfStyle.Money(i.Importe)}"));
                    col.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Detalle por medio: ").Bold().FontSize(7);
                        t.Span(detalle).FontSize(7);
                    });
                }
            });
    }

    private static void Kpi(RowDescriptor row, string label, decimal value, bool bold = false)
    {
        row.RelativeItem().Column(col =>
        {
            col.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
            var text = col.Item().Text(CajaSaldosPdfStyle.Money(value)).FontSize(10);
            if (bold)
                text.Bold().FontColor(CajaSaldosPdfStyle.Brand);
        });
    }

    private static void ComposeTable(IContainer container, IReadOnlyList<RptMovimientoBovedaRowDto> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(82);
                columns.ConstantColumn(48);
                columns.RelativeColumn(2.2f);
                columns.ConstantColumn(68);
                columns.ConstantColumn(68);
                columns.ConstantColumn(64);
                columns.RelativeColumn(1.4f);
            });

            table.Header(h =>
            {
                foreach (var label in new[]
                {
                    "Fecha", "Cód.", "Glosa", "Entrada", "Salida", "Tipo pago", "Agente",
                })
                {
                    h.Cell().Element(CajaSaldosPdfStyle.HeaderCell)
                        .AlignCenter()
                        .Text(label).Bold().FontColor(Colors.White).FontSize(7.2f);
                }
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = i % 2 == 0 ? Colors.White : CajaSaldosPdfStyle.Zebra;
                Cell(table, CajaSaldosPdfStyle.DateTime(r.FechaReg), bg, center: true);
                Cell(table, r.CodOperacion, bg, center: true);
                Cell(table, r.Glosa, bg);
                Money(table, r.Entrada, bg);
                Money(table, r.Salida, bg);
                Cell(table, r.TipoPago, bg, center: true);
                Cell(table, r.Agente, bg);
            }

            table.Cell().ColumnSpan(3).Element(CajaSaldosPdfStyle.TotalsCell)
                .AlignRight().Text("TOTAL").Bold();
            Money(table, rows.Sum(r => r.Entrada ?? 0m), CajaSaldosPdfStyle.TotalsBg, totals: true);
            Money(table, rows.Sum(r => r.Salida ?? 0m), CajaSaldosPdfStyle.TotalsBg, totals: true);
            table.Cell().ColumnSpan(2).Element(CajaSaldosPdfStyle.TotalsCell);
        });
    }

    private static void Cell(TableDescriptor table, string? value, Color bg, bool center = false)
    {
        var cell = table.Cell().Element(c => CajaSaldosPdfStyle.BodyCell(c, bg));
        if (center)
            cell = cell.AlignCenter();
        cell.Text(value ?? string.Empty).FontSize(7.2f);
    }

    private static void Money(TableDescriptor table, decimal? value, Color bg, bool totals = false)
    {
        table.Cell()
            .Element(c => totals ? CajaSaldosPdfStyle.TotalsCell(c) : CajaSaldosPdfStyle.BodyCell(c, bg))
            .AlignRight()
            .Text(value is null or 0 ? string.Empty : CajaSaldosPdfStyle.Money(value.Value))
            .FontSize(7.2f)
            .Bold();
    }
}
