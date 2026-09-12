using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Plantilla PDF profesional para Cobro Diario. Sustituye el PDF tabular genérico
/// por un layout de impresión inspirado en el legacy, mantenible con QuestPDF.
/// </summary>
public static class RptCobroDiarioPdfDocument
{
    private static readonly Color Brand = Color.FromHex("#114885");
    private static readonly Color HeaderBg = Color.FromHex("#0F5F8F");
    private static readonly Color SectionBg = Color.FromHex("#EAF4FB");
    private static readonly Color Border = Color.FromHex("#B7C7D6");
    private static readonly Color Zebra = Color.FromHex("#F8FBFD");

    static RptCobroDiarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptCobroDiarioRowDto> rows,
        CredixLegacyReportContext context,
        bool soloMora)
    {
        var logo = CredixReportAssets.LoadLogo();
        var culture = CultureInfo.CurrentCulture;
        var inv = CultureInfo.InvariantCulture;
        var title = soloMora ? "MOROSIDAD POR GESTOR" : "COBRO DIARIO";
        var printedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm", culture);
        var saldoPendiente = rows.Sum(x => x.Saldo ?? 0m);
        var saldoVencido = rows.Where(x => (x.DiasAtrazo ?? 0) > 0).Sum(x => x.Saldo ?? 0m);
        var saldoMora = rows.Sum(x => x.Mora ?? 0m);
        var moraPct = saldoPendiente > 0 ? saldoMora / saldoPendiente : 0m;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(14);
                page.DefaultTextStyle(s => s.FontSize(5.4f).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(
                        c,
                        logo,
                        title,
                        printedAt,
                        context,
                        rows.Count,
                        saldoVencido,
                        saldoPendiente,
                        moraPct,
                        culture));
                });

                page.Content().PaddingTop(6).Element(c => ComposeTable(c, rows, culture, inv));

                page.Footer().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Crediconfiable - reporte generado por Credito Modern")
                        .FontSize(6)
                        .FontColor(Colors.Grey.Darken2);
                    row.RelativeItem().AlignRight().DefaultTextStyle(x => x.FontSize(6)).Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        byte[] logo,
        string title,
        string printedAt,
        CredixLegacyReportContext context,
        int clientes,
        decimal saldoVencido,
        decimal saldoPendiente,
        decimal moraPct,
        CultureInfo culture)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(92).Height(34).Image(logo).FitArea();
                row.RelativeItem().AlignMiddle().AlignCenter().Text(title)
                    .Bold()
                    .FontSize(11)
                    .FontColor(Colors.Black);
                row.ConstantItem(118).AlignMiddle().AlignRight().Text(printedAt)
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken3);
            });

            col.Item().PaddingTop(4).Border(0.5f).BorderColor(Border).Background(SectionBg).Padding(5).Column(meta =>
            {
                meta.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Oficina: ").Bold();
                        t.Span(string.IsNullOrWhiteSpace(context.Oficina) ? "—" : context.Oficina);
                    });
                    row.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Gestor: ").Bold();
                        t.Span(context.Agente ?? "TODOS");
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Caja: ").Bold();
                        t.Span(string.IsNullOrWhiteSpace(context.Caja) ? "—" : context.Caja);
                    });
                });

                meta.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Fecha: ").Bold();
                        t.Span(string.IsNullOrWhiteSpace(context.Fecha) ? "—" : context.Fecha);
                    });
                    row.RelativeItem().AlignCenter();
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("N° clientes: ").Bold();
                        t.Span(clientes.ToString(CultureInfo.InvariantCulture));
                    });
                });

                meta.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Saldo Vencido: ").Bold();
                        t.Span(saldoVencido.ToString("N2", culture));
                    });
                    row.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Saldo Pendiente: ").Bold();
                        t.Span(saldoPendiente.ToString("N2", culture));
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Mora Total: ").Bold();
                        t.Span(moraPct.ToString("P1", culture));
                    });
                });
            });
        });
    }

    private static void ComposeTable(
        IContainer container,
        IReadOnlyList<RptCobroDiarioRowDto> rows,
        CultureInfo culture,
        CultureInfo inv)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(16); // Nro
                columns.RelativeColumn(2.25f); // Cliente
                columns.ConstantColumn(38); // Monto recibido
                columns.ConstantColumn(44); // Firma
                columns.RelativeColumn(2.3f); // Dirección
                columns.RelativeColumn(1.55f); // Negocio
                columns.ConstantColumn(36); // Celular
                columns.ConstantColumn(26); // Tope
                columns.ConstantColumn(24); // SBS
                columns.ConstantColumn(36); // F. inicio
                columns.ConstantColumn(22); // Cuot. a pen
                columns.ConstantColumn(38); // Monto crédito
                columns.ConstantColumn(18); // %
                columns.ConstantColumn(18); // F
                columns.ConstantColumn(32); // Cuota plan
                columns.ConstantColumn(36); // Monto total
                columns.ConstantColumn(36); // Fecha pago
                columns.ConstantColumn(32); // Saldo
                columns.ConstantColumn(24); // Días
                columns.ConstantColumn(36); // F. venc.
                columns.ConstantColumn(28); // Mora
                columns.ConstantColumn(32); // Cuota total
            });

            table.Header(header =>
            {
                foreach (var h in Headers)
                {
                    header.Cell().Element(HeaderCell).Text(h).Bold().FontSize(5.2f).FontColor(Colors.White);
                }
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = i % 2 == 0 ? Colors.White : Zebra;
                Cell(table, r.Nro?.ToString(inv) ?? string.Empty, bg, true);
                Cell(table, r.Cliente ?? string.Empty, bg);
                Cell(table, string.Empty, bg, true);
                Cell(table, string.Empty, bg);
                Cell(table, r.Direccion ?? string.Empty, bg);
                Cell(table, r.Negocio ?? string.Empty, bg);
                Cell(table, r.Celular ?? string.Empty, bg, true);
                Cell(table, Money(r.TopeCredito, culture), bg, true);
                Cell(table, r.ClasificacionRiesgoSBS ?? string.Empty, bg, true);
                Cell(table, Date(r.FechaPrimerPago, culture), bg, true);
                Cell(table, r.NroCuotasPen?.ToString(inv) ?? string.Empty, bg, true);
                Cell(table, r.MontoCredito.ToString("N2", culture), bg, true);
                Cell(table, r.Interes.ToString("N0", culture) + "%", bg, true);
                Cell(table, r.FormaPago, bg, true);
                Cell(table, Money(r.CuotaPlan, culture), bg, true);
                Cell(table, Money(r.MontoTotal, culture), bg, true);
                Cell(table, Date(r.FechaPago, culture), bg, true);
                Cell(table, Money(r.Saldo, culture), bg, true);
                Cell(table, r.DiasAtrazo?.ToString(inv) ?? string.Empty, bg, true);
                Cell(table, Date(r.FechaVencimiento, culture), bg, true);
                Cell(table, Money(r.Mora, culture), bg, true);
                Cell(table, Money(r.CuotaTotal, culture), bg, true);
            }
        });
    }

    private static readonly string[] Headers =
    [
        "N°",
        "Cliente",
        "Monto\nrecibido",
        "Firma",
        "Dirección",
        "Negocio",
        "Celular",
        "Tope",
        "SBS",
        "Fecha\ninicio",
        "Cuot.\na pen",
        "Monto\ncrédito",
        "%",
        "F",
        "Cuota\nplan",
        "Monto\ntotal",
        "Fecha\npago",
        "Saldo",
        "Días\natr.",
        "Fecha\nvenc.",
        "Mora",
        "Cuota\ntotal",
    ];

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(0.4f)
            .BorderColor(Colors.White)
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle();

    private static void Cell(TableDescriptor table, string value, Color bg, bool right = false)
    {
        var cell = table.Cell()
            .Background(bg)
            .Border(0.35f)
            .BorderColor(Border)
            .PaddingVertical(2)
            .PaddingHorizontal(2)
            .AlignMiddle();

        if (right)
        {
            cell = cell.AlignRight();
        }

        cell.Text(value).FontSize(5.1f);
    }

    private static string Money(decimal? value, CultureInfo culture) =>
        value.HasValue ? value.Value.ToString("N2", culture) : string.Empty;

    private static string Date(DateTime? value, CultureInfo culture) =>
        value.HasValue ? value.Value.ToString("dd/MM/yyyy", culture) : string.Empty;
}
