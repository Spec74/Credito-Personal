using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de saldo de una sesión: paridad <c>rptSaldoCaja.rdlc</c> (A4 vertical, cabecera de
/// oficina/cajero/saldos, grupos INGRESOS / EGRESOS y totales).
/// </summary>
public static class RptSaldosCajaPdfDocument
{
    static RptSaldosCajaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptSaldosCajaRowDto> rows,
        RptSaldoCajaCabDto? cab,
        string? resumenIngreso,
        bool cajaChica)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CajaSaldosPdfStyle.Pe);
        var fechaTitulo = cab is null ? printedAt : CajaSaldosPdfStyle.Date(cab.Fecha);
        var title = cajaChica
            ? $"SALDOS DE CAJA CHICA DEL {fechaTitulo}"
            : $"SALDOS DE CAJA DEL {fechaTitulo}";

        var ingresos = rows.Where(r => r.IndEntrada).ToList();
        var egresos = rows.Where(r => !r.IndEntrada).ToList();

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
                        CajaSaldosPdfStyle.TitleBand(c, logo, title, printedAt));
                    if (cab is not null)
                    {
                        col.Item().PaddingTop(6).Element(c => ComposeCab(c, cab, resumenIngreso));
                    }
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c => ComposeGrupo(c, "INGRESOS", ingresos));
                    col.Item().PaddingTop(8).Element(c => ComposeGrupo(c, "EGRESOS", egresos));
                    col.Item().PaddingTop(16).Element(ComposeFirmas);
                });

                CajaSaldosPdfStyle.Footer(page, rows.Count);
            });
        }).GeneratePdf();
    }

    private static void ComposeCab(
        IContainer container,
        RptSaldoCajaCabDto cab,
        string? resumenIngreso)
    {
        var composicion = ResumenCuentaCajaParser.Compose(resumenIngreso);

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
                        t.Span(cab.Oficina);
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Estado: ").Bold();
                        t.Span(cab.Estado);
                    });
                });
                col.Item().PaddingTop(2).Text(t =>
                {
                    t.Span("Cajero: ").Bold();
                    t.Span(cab.Cajero);
                });
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Saldo inicial: ").Bold();
                        t.Span(CajaSaldosPdfStyle.Money(cab.SaldoInicial));
                    });
                    row.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Saldo final: ").Bold();
                        t.Span(CajaSaldosPdfStyle.Money(cab.SaldoFinal));
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("% cobro: ").Bold();
                        t.Span(cab.PorcentajeCobro.ToString("N1", CajaSaldosPdfStyle.Pe) + " %");
                    });
                });

                // Misma composición que bóveda / pantalla caja: efectivo vs digitales + detalle.
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
                else if (!string.IsNullOrWhiteSpace(resumenIngreso))
                {
                    col.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Resumen de ingresos: ").Bold();
                        t.Span(ResumenCuentaCajaParser.FormatLine(resumenIngreso, CajaSaldosPdfStyle.Pe));
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

    private static void ComposeGrupo(
        IContainer container,
        string titulo,
        IReadOnlyList<RptSaldosCajaRowDto> rows)
    {
        container.Column(col =>
        {
            col.Item().Background(CajaSaldosPdfStyle.HeaderBg)
                .Padding(4)
                .Text(titulo)
                .Bold()
                .FontColor(Colors.White)
                .FontSize(8.5f);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(48);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(88);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(2.0f);
                    columns.ConstantColumn(68);
                    columns.ConstantColumn(72);
                });

                table.Header(h =>
                {
                    foreach (var label in new[]
                    {
                        "Mov.", "Op.", "Fecha", "Cliente", "Glosa", "Importe", "Tipo pago",
                    })
                    {
                        h.Cell().Element(CajaSaldosPdfStyle.HeaderCell)
                            .AlignCenter()
                            .Text(label).Bold().FontColor(Colors.White).FontSize(7);
                    }
                });

                for (var i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    var bg = i % 2 == 0 ? Colors.White : CajaSaldosPdfStyle.Zebra;
                    Cell(table, r.MovimientoCajaId.ToString(System.Globalization.CultureInfo.InvariantCulture), bg, center: true);
                    Cell(table, r.Operacion, bg, center: true);
                    Cell(table, CajaSaldosPdfStyle.DateTime(r.FechaReg), bg, center: true);
                    Cell(table, r.Cliente, bg);
                    Cell(table, r.Glosa, bg);
                    Money(table, r.ImportePago, bg);
                    Cell(table, r.TipoPago, bg, center: true);
                }

                table.Cell().ColumnSpan(5).Element(CajaSaldosPdfStyle.TotalsCell)
                    .AlignRight()
                    .Text(titulo == "INGRESOS" ? "Total ingreso" : "Total egreso")
                    .Bold();
                Money(table, rows.Sum(r => r.ImportePago), CajaSaldosPdfStyle.TotalsBg, totals: true);
                table.Cell().Element(CajaSaldosPdfStyle.TotalsCell);
            });
        });
    }

    private static void ComposeFirmas(IContainer container)
    {
        container.PaddingTop(8).Row(row =>
        {
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().Width(160).LineHorizontal(0.6f).LineColor(CajaSaldosPdfStyle.Brand);
                col.Item().PaddingTop(3).Text("Gerente").FontSize(8);
            });
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().Width(160).LineHorizontal(0.6f).LineColor(CajaSaldosPdfStyle.Brand);
                col.Item().PaddingTop(3).Text("Gestor").FontSize(8);
            });
        });
    }

    private static void Cell(TableDescriptor table, string? value, Color bg, bool center = false)
    {
        var cell = table.Cell().Element(c => CajaSaldosPdfStyle.BodyCell(c, bg));
        if (center)
            cell = cell.AlignCenter();
        cell.Text(value ?? string.Empty).FontSize(7);
    }

    private static void Money(TableDescriptor table, decimal value, Color bg, bool totals = false)
    {
        table.Cell()
            .Element(c => totals ? CajaSaldosPdfStyle.TotalsCell(c) : CajaSaldosPdfStyle.BodyCell(c, bg))
            .AlignRight()
            .Text(CajaSaldosPdfStyle.Money(value))
            .FontSize(7)
            .Bold();
    }
}
