using System.Globalization;
using Credito.Modern.Application.CreditoPlanes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// PDF de morosidad — basado en <c>rptCreditoMorosidad.rdlc</c>, con subfila de contacto legible.
/// </summary>
public static class CredixMorosidadPdfDocument
{
    private const int FinancialColumnCount = 14;

    static CredixMorosidadPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public sealed record Header(
        string Oficina,
        string HastaFecha,
        int DiasAtrazoIni,
        int DiasAtrazoFin);

    public static byte[] Build(IReadOnlyList<RptCreditoMorosidadRowDto> rows, Header header)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        var inv = CultureInfo.InvariantCulture;
        var title = $"REPORTE CREDITOS DESEMBOLSADOS AL {header.HastaFecha}";
        var metadata = new CredixLegacyPdfDocument.MetadataLine[]
        {
            new("Oficina: ", header.Oficina),
            new("Fecha corte: ", header.HastaFecha),
            new("Días de atraso: ", $"{header.DiasAtrazoIni} al {header.DiasAtrazoFin}"),
        };

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(s => s.FontSize(CredixLegacyPdfDocument.FontSizeBody));

                page.Header().Column(col =>
                {
                    col.Item().Element(c =>
                        CredixLegacyPdfDocument.ComposeTitleBand(c, logo, title, printedAt));
                    col.Item().PaddingTop(4).Element(c =>
                        CredixLegacyPdfDocument.ComposeMetadataBlock(c, metadata));
                });

                page.Content().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(DefineColumns);

                    table.Header(h =>
                    {
                        void H(string label) =>
                            h.Cell().Element(CredixLegacyPdfDocument.HeaderCell)
                                .Text(label).Bold().FontSize(CredixLegacyPdfDocument.FontSizeBody);

                        H("N° créd.");
                        H("Artículo");
                        H("Fecha Desembolso");
                        H("Fecha Vencimiento");
                        H("Crédito");
                        H("Saldo Crédito");
                        H("Capital Atrazo");
                        H("Gastos Adm");
                        H("Interés Atrazo");
                        H("Mora");
                        H("Importe Libre");
                        H("Días Atrazo");
                        H("Cuotas Atrazo");
                        H("Deuda Atrazo");
                    });

                    foreach (var r in rows)
                    {
                        ComposeFinancialRow(table, r, inv);
                        ComposeContactSubRow(table, r);
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignLeft().Text(t =>
                    {
                        t.Span("Filas: ");
                        t.Span(rows.Count.ToString(inv)).Bold();
                    });
                    row.RelativeItem().AlignCenter().Text(t =>
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

    private static void DefineColumns(TableColumnsDefinitionDescriptor cols)
    {
        cols.ConstantColumn(32);
        cols.RelativeColumn(1.4f);
        cols.ConstantColumn(52);
        cols.ConstantColumn(52);
        cols.ConstantColumn(48);
        cols.ConstantColumn(48);
        cols.ConstantColumn(44);
        cols.ConstantColumn(40);
        cols.ConstantColumn(44);
        cols.ConstantColumn(36);
        cols.ConstantColumn(44);
        cols.ConstantColumn(36);
        cols.ConstantColumn(36);
        cols.ConstantColumn(48);
    }

    /// <summary>Fila 1: crédito, artículo vendido y montos (como fila de detalle del RDLC).</summary>
    private static void ComposeFinancialRow(
        TableDescriptor table,
        RptCreditoMorosidadRowDto r,
        IFormatProvider inv)
    {
        void TextCell(string? value, bool center = false, bool numeric = false)
        {
            IContainer Style(IContainer c) =>
                numeric
                    ? CredixLegacyPdfDocument.BodyCellNumeric(c)
                    : center
                        ? CredixLegacyPdfDocument.BodyCellCenter(c)
                        : CredixLegacyPdfDocument.BodyCell(c);

            table.Cell().Element(Style).Text(value ?? string.Empty)
                .FontSize(CredixLegacyPdfDocument.FontSizeBody);
        }

        TextCell(r.CreditoId.ToString(inv), center: true);
        TextCell(string.IsNullOrWhiteSpace(r.Articulo) ? "—" : r.Articulo);
        TextCell(FormatDate(r.FechaDesembolso), center: true);
        TextCell(FormatDate(r.FechaVcto), center: true);
        TextCell(FormatDecimal(r.MontoCredito), numeric: true);
        TextCell(FormatDecimal(r.SaldoCredito), numeric: true);
        TextCell(FormatDecimal(r.CapitalAtrazo), numeric: true);
        TextCell(FormatDecimal(r.GA), numeric: true);
        TextCell(FormatDecimal(r.InteresAtrazo), numeric: true);
        TextCell(FormatDecimal(r.Mora), numeric: true);
        TextCell(FormatDecimal(r.ImporteLibre), numeric: true);
        TextCell(r.DiasAtrazo?.ToString(inv) ?? string.Empty, center: true);
        TextCell(r.CuotasAtrazo?.ToString(inv) ?? string.Empty, center: true);
        TextCell(FormatDecimal(r.DeudaAtrazo), numeric: true);
    }

    /// <summary>
    /// Fila 2: datos de contacto con etiquetas (mejora al RDLC, que mezclaba cliente bajo columna Artículo).
    /// Legacy: col. Cred vacía; col. Artículo = cliente+cel; cols. 3–14 = dirección.
    /// </summary>
    private static void ComposeContactSubRow(TableDescriptor table, RptCreditoMorosidadRowDto r)
    {
        table.Cell().Element(CredixLegacyPdfDocument.BodyCellContactBand).Text(string.Empty);

        table.Cell().ColumnSpan(FinancialColumnCount - 1)
            .Element(CredixLegacyPdfDocument.BodyCellContactBand)
            .Row(sub =>
            {
                sub.RelativeItem(4).Element(c => ComposeLabeledField(c, "Cliente", r.Cliente));
                sub.ConstantItem(118).Element(c => ComposeLabeledField(c, "Celular", r.Celular));
                sub.RelativeItem(5).Element(c => ComposeLabeledField(c, "Dirección", r.Direccion));
            });
    }

    private static void ComposeLabeledField(IContainer container, string label, string? value)
    {
        container.Text(text =>
        {
            text.Span($"{label}: ").SemiBold().FontSize(CredixLegacyPdfDocument.FontSizeBody);
            text.Span(string.IsNullOrWhiteSpace(value) ? "—" : value)
                .FontSize(CredixLegacyPdfDocument.FontSizeBody);
        });
    }

    private static string FormatDate(DateTime? d) =>
        d is null ? string.Empty : d.Value.ToString("d", CultureInfo.CurrentCulture);

    private static string FormatDecimal(decimal? v) =>
        v is null ? string.Empty : v.Value.ToString("N2", CultureInfo.CurrentCulture);

    private static string FormatDecimal(decimal v) =>
        v.ToString("N2", CultureInfo.CurrentCulture);
}
