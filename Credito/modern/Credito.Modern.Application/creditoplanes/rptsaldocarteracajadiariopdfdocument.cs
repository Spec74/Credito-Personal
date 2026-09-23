using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de saldo cartera / caja diario: hoja extra-ancha, cabecera agrupada
/// (periodo inicial vs final) para que no se apile letra por letra como el tabular A4.
/// </summary>
public static class RptSaldoCarteraCajaDiarioPdfDocument
{
    private static readonly Color HeaderBg = CredixReportTokens.TableHeader;
    private static readonly Color GroupIniBg = CredixReportTokens.SoftBg;
    private static readonly Color GroupFinBg = CredixReportTokens.TotalsBg;
    private static readonly Color BorderColor = CredixReportTokens.Border;
    private static readonly Color Zebra = CredixReportTokens.MetaBg;

    static RptSaldoCarteraCajaDiarioPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public sealed record Header(string Oficina, string PeriodoIni, string PeriodoFin);

    public static byte[] Build(IReadOnlyList<RptSaldoCarteraCajaDiarioRowDto> rows, Header header)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = CredixReportTokens.NowPrinted();
        var title = "SALDO CARTERA Y CAJA DIARIO";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(new PageSize(1480, 842));
                page.Margin(14);
                page.DefaultTextStyle(s => s.FontSize(6.4f));

                page.Header().Column(col =>
                {
                    col.Item().Element(c =>
                        CredixLegacyPdfDocument.ComposeTitleBand(c, logo, title, printedAt));
                    col.Item().PaddingTop(4).Element(c =>
                        CredixLegacyPdfDocument.ComposeMetadataBlock(
                            c,
                            [
                                new CredixLegacyPdfDocument.MetadataLine("Oficina: ", header.Oficina),
                                new CredixLegacyPdfDocument.MetadataLine("Periodo inicial: ", header.PeriodoIni),
                                new CredixLegacyPdfDocument.MetadataLine("Periodo final: ", header.PeriodoFin),
                            ]));
                });

                page.Content().PaddingTop(6).Element(c => ComposeTable(c, rows, CredixReportTokens.Pe));

                page.Footer().Element(c => CredixReportTokens.ComposeStandardFooter(c, rows.Count));
            });
        }).GeneratePdf();
    }

    private static void ComposeTable(
        IContainer container,
        IReadOnlyList<RptSaldoCarteraCajaDiarioRowDto> rows,
        CultureInfo culture)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.35f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.45f);
                for (var i = 0; i < 22; i++)
                    columns.RelativeColumn(0.82f);
            });

            table.Header(h =>
            {
                h.Cell().ColumnSpan(3).Element(c => GroupCell(c, HeaderBg))
                    .Text("Identificación").Bold().FontSize(6.6f);
                h.Cell().ColumnSpan(11).Element(c => GroupCell(c, GroupIniBg))
                    .Text("PERIODO INICIAL").Bold().FontSize(6.8f);
                h.Cell().ColumnSpan(11).Element(c => GroupCell(c, GroupFinBg))
                    .Text("PERIODO FINAL").Bold().FontSize(6.8f);

                foreach (var label in SubHeaders)
                    h.Cell().Element(SubHeaderCell).Text(label).Bold().FontSize(5.8f);
            });

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var bg = i % 2 == 0 ? Colors.White : Zebra;
                Text(table, r.Oficina, bg);
                Text(table, r.Caja, bg);
                Text(table, r.Agente, bg);
                Text(table, Date(r.FechaCierreIni, culture), bg, center: true);
                Money(table, r.SalidasIni, bg, culture);
                Money(table, r.MontoCobradoIni, bg, culture);
                Pct(table, r.PocentajeCobroIni, bg, culture);
                Money(table, r.SaldoCarteraSinMoraIni, bg, culture);
                Int(table, r.NroClientesCarteraSinMoraIni, bg);
                Money(table, r.SaldoMoraCarteraIni, bg, culture);
                Int(table, r.NroClientesSaldoMoraCarteraIni, bg);
                Int(table, r.NroClientesNuevosIni, bg);
                Money(table, r.SaldoVencidoIni, bg, culture);
                Money(table, r.SaldoMorosidadIni, bg, culture);
                Text(table, Date(r.FechaCierreFin, culture), bg, center: true);
                Money(table, r.SalidasFin, bg, culture);
                Money(table, r.MontoCobradoFin, bg, culture);
                Pct(table, r.PocentajeCobroFin, bg, culture);
                Money(table, r.SaldoCarteraSinMoraFin, bg, culture);
                Int(table, r.NroClientesCarteraSinMoraFin, bg);
                Money(table, r.SaldoMoraCarteraFin, bg, culture);
                Int(table, r.NroClientesSaldoMoraCarteraFin, bg);
                Int(table, r.NroClientesNuevosFin, bg);
                Money(table, r.SaldoVencidoFin, bg, culture);
                Money(table, r.SaldoMorosidadFin, bg, culture);
            }
        });
    }

    private static readonly string[] SubHeaders =
    [
        "Oficina",
        "Caja",
        "Agente",
        "Cierre",
        "Desembolsos",
        "Cobrado",
        "% cobro",
        "Cart. s/mora",
        "Cli. s/mora",
        "Mora cart.",
        "Cli. mora",
        "Cli. nuevos",
        "Vencido",
        "Morosidad",
        "Cierre",
        "Desembolsos",
        "Cobrado",
        "% cobro",
        "Cart. s/mora",
        "Cli. s/mora",
        "Mora cart.",
        "Cli. mora",
        "Cli. nuevos",
        "Vencido",
        "Morosidad",
    ];

    private static IContainer GroupCell(IContainer container, Color bg) =>
        container
            .Background(bg)
            .Border(CredixLegacyPdfDocument.HeaderBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(3)
            .AlignCenter()
            .AlignMiddle();

    private static IContainer SubHeaderCell(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(CredixLegacyPdfDocument.HeaderBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle();

    private static void Text(TableDescriptor table, string? value, Color bg, bool center = false)
    {
        var cell = table.Cell()
            .Background(bg)
            .Border(CredixLegacyPdfDocument.BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(2)
            .PaddingHorizontal(2)
            .AlignMiddle();
        if (center)
            cell = cell.AlignCenter();
        cell.Text(value ?? string.Empty);
    }

    private static void Money(TableDescriptor table, decimal? value, Color bg, CultureInfo culture)
    {
        var cell = table.Cell()
            .Background(bg)
            .Border(CredixLegacyPdfDocument.BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(2)
            .PaddingHorizontal(2)
            .AlignMiddle()
            .AlignRight();
        cell.Text(value is null ? string.Empty : value.Value.ToString("N2", culture));
    }

    private static void Pct(TableDescriptor table, decimal? value, Color bg, CultureInfo culture)
    {
        var cell = table.Cell()
            .Background(bg)
            .Border(CredixLegacyPdfDocument.BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(2)
            .PaddingHorizontal(2)
            .AlignMiddle()
            .AlignRight();
        cell.Text(value is null ? string.Empty : value.Value.ToString("N1", culture) + "%");
    }

    private static void Int(TableDescriptor table, int value, Color bg)
    {
        var cell = table.Cell()
            .Background(bg)
            .Border(CredixLegacyPdfDocument.BodyBorderPt)
            .BorderColor(BorderColor)
            .PaddingVertical(2)
            .PaddingHorizontal(2)
            .AlignMiddle()
            .AlignCenter();
        cell.Text(value.ToString(CultureInfo.InvariantCulture));
    }

    private static string Date(DateTime? value, CultureInfo culture) =>
        value is null ? string.Empty : value.Value.ToString("dd/MM/yyyy", culture);
}
