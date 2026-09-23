using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF corporativo de clientes inactivos — paridad visual/datos con
/// <c>rptClientesInactivos.rdlc</c> / <c>ClienteBL.ReporteClientesInactivos</c>.
/// </summary>
public static class RptClientesInactivosFichaPdfDocument
{
    private static readonly Color Brand = CredixReportTokens.Brand;
    private static readonly Color TitleBar = CredixReportTokens.BrandDark;
    private static readonly Color HeaderBg = CredixReportTokens.TableHeaderStrong;
    private static readonly Color MetaBg = CredixReportTokens.SoftBg;
    private static readonly Color Border = CredixReportTokens.BorderSoft;
    private static readonly Color Zebra = CredixReportTokens.Zebra;
    private static readonly Color TotalsBg = CredixReportTokens.TotalsBg;

    private static readonly string[] Headers =
    [
        "Nro",
        "Agente",
        "Dni",
        "Cliente",
        "Direccion",
        "Direccion ref",
        "Celular",
        "Cal",
        "SBS",
        "Depurado",
        "Direccion negocio",
        "Direccion Negocio ref",
        "Monto Credito",
        "Cantidad\ncreditos",
        "Fecha\ncancelacion",
        "Tope credito",
    ];

    static RptClientesInactivosFichaPdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(
        IReadOnlyList<RptClientesInactivosRowDto> rows,
        CredixLegacyReportContext? context = null)
    {
        var logo = CredixReportAssets.LoadLogo();
        var cult = CredixReportTokens.Pe;
        var inv = CredixReportTokens.Inv;
        var printedAt = CredixReportTokens.NowPrinted();
        var fechaCorte = ResolveFechaCorte(context, cult);
        var oficina = NormalizeLabel(context?.Oficina);
        var agente = NormalizeLabel(context?.Agente ?? context?.Gestor);
        var totalMonto = rows.Sum(x => x.MontoCredito);
        var totalTope = rows.Sum(x => x.TopeCredito);
        var totalCreditos = rows.Sum(x => x.TotalCreditos);
        var title = $"REPORTE CLIENTES INACTIVOS AL {fechaCorte}";

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(12);
                page.DefaultTextStyle(s => s.FontSize(5.6f));

                page.Header().Element(c => ComposeHeader(
                    c,
                    logo,
                    title,
                    printedAt,
                    agente,
                    oficina,
                    rows.Count));

                page.Content().PaddingTop(5).Column(col =>
                {
                    if (rows.Count == 0)
                    {
                        col.Item().PaddingTop(18).AlignCenter()
                            .Text("No hay clientes inactivos para los filtros indicados.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(DefineColumns);

                        table.Header(h =>
                        {
                            foreach (var label in Headers)
                            {
                                h.Cell().Element(HeaderCell)
                                    .Text(label)
                                    .Bold()
                                    .FontSize(5.3f)
                                    .FontColor(Colors.White);
                            }
                        });

                        for (var i = 0; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var bg = i % 2 == 0 ? Colors.White : Zebra;
                            var nro = (i + 1).ToString(inv);

                            Cell(table, nro, bg, center: true);
                            Cell(table, Text(r.Agente), bg);
                            Cell(table, Text(r.Dni), bg, center: true);
                            Cell(table, Text(r.Cliente), bg);
                            Cell(table, Text(r.Direccion), bg);
                            Cell(table, Text(r.DireccionRef), bg);
                            Cell(table, Text(r.Celular), bg, center: true);
                            Cell(table, Text(r.Calificacion), bg, center: true);
                            Cell(table, Text(r.ClasificacionRiesgoSBS), bg, center: true);
                            Cell(table, Text(r.Depurado), bg, center: true);
                            Cell(table, Text(r.DireccionNegocio), bg);
                            Cell(table, Text(r.DireccionNegocioRef), bg);
                            Cell(table, CredixReportTokens.FormatMoney(r.MontoCredito), bg, right: true);
                            Cell(table, r.TotalCreditos.ToString(inv), bg, center: true);
                            Cell(table, CredixReportTokens.FormatDate(r.FechaCancelacion), bg, center: true);
                            Cell(table, CredixReportTokens.FormatMoney(r.TopeCredito), bg, right: true);
                        }

                        // Totales — columnas financieras alineadas al legacy.
                        TotalCell(table, "TOTAL", span: 12);
                        TotalCell(table, CredixReportTokens.FormatMoney(totalMonto), right: true);
                        TotalCell(table, totalCreditos.ToString(inv), center: true);
                        TotalCell(table, string.Empty);
                        TotalCell(table, CredixReportTokens.FormatMoney(totalTope), right: true);
                    });
                });

                page.Footer().Element(c => CredixReportTokens.ComposeStandardFooter(c, rows.Count));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        byte[] logo,
        string title,
        string printedAt,
        string agente,
        string oficina,
        int totalClientes)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(100).Height(36).Image(logo).FitArea();
                row.RelativeItem().AlignMiddle().PaddingHorizontal(8).Column(t =>
                {
                    t.Item().AlignCenter().Text(CredixReportTokens.CompanyLegalName)
                        .FontSize(8)
                        .FontColor(Brand)
                        .SemiBold();
                    t.Item().PaddingTop(3)
                        .Background(TitleBar)
                        .PaddingVertical(6)
                        .PaddingHorizontal(10)
                        .AlignCenter()
                        .Text(title)
                        .Bold()
                        .FontSize(11)
                        .FontColor(Colors.White);
                });
                row.ConstantItem(110).AlignMiddle().AlignRight().Text(printedAt)
                    .FontSize(7)
                    .FontColor(Colors.Grey.Darken2);
            });

            col.Item().PaddingTop(5)
                .Border(0.6f)
                .BorderColor(Border)
                .Background(MetaBg)
                .PaddingVertical(5)
                .PaddingHorizontal(8)
                .Row(meta =>
                {
                    meta.RelativeItem().Text(t =>
                    {
                        t.Span("Agente: ").Bold().FontSize(7.2f);
                        t.Span(agente).FontSize(7.2f);
                    });
                    meta.RelativeItem().AlignCenter().Text(t =>
                    {
                        t.Span("Oficina: ").Bold().FontSize(7.2f);
                        t.Span(oficina).FontSize(7.2f);
                    });
                    meta.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Total de clientes: ").Bold().FontSize(7.2f);
                        t.Span(totalClientes.ToString(CredixReportTokens.Inv)).FontSize(7.2f);
                    });
                });
        });
    }

    private static void DefineColumns(TableColumnsDefinitionDescriptor c)
    {
        c.ConstantColumn(18);   // Nro
        c.ConstantColumn(36);   // Agente
        c.ConstantColumn(42);   // Dni
        c.RelativeColumn(1.55f); // Cliente
        c.RelativeColumn(1.35f); // Direccion
        c.RelativeColumn(1.25f); // Direccion ref
        c.ConstantColumn(40);   // Celular
        c.ConstantColumn(18);   // Cal
        c.ConstantColumn(36);   // SBS
        c.ConstantColumn(34);   // Depurado
        c.RelativeColumn(1.15f); // Dir negocio
        c.RelativeColumn(1.1f);  // Dir negocio ref
        c.ConstantColumn(46);   // Monto
        c.ConstantColumn(34);   // Cant créditos
        c.ConstantColumn(44);   // Fecha cancel
        c.ConstantColumn(44);   // Tope
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(HeaderBg)
            .Border(0.4f)
            .BorderColor(Colors.White)
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle();

    private static void Cell(
        TableDescriptor table,
        string value,
        Color bg,
        bool right = false,
        bool center = false)
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
        else if (center)
        {
            cell = cell.AlignCenter();
        }

        cell.Text(value).FontSize(5.2f);
    }

    private static void TotalCell(
        TableDescriptor table,
        string value,
        int span = 1,
        bool right = false,
        bool center = false)
    {
        var cell = table.Cell();
        if (span > 1)
        {
            cell = cell.ColumnSpan((uint)span);
        }

        var styled = cell
            .Background(TotalsBg)
            .Border(0.45f)
            .BorderColor(Border)
            .BorderTop(1f)
            .BorderColor(Brand)
            .PaddingVertical(3)
            .PaddingHorizontal(2)
            .AlignMiddle()
            .DefaultTextStyle(x => x.SemiBold().FontSize(5.4f));

        if (right)
        {
            styled = styled.AlignRight();
        }
        else if (center)
        {
            styled = styled.AlignCenter();
        }

        styled.Text(value);
    }

    private static string Text(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeLabel(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() is "—" or "-"
            ? "TODOS"
            : value.Trim();

    private static string ResolveFechaCorte(CredixLegacyReportContext? context, CultureInfo cult)
    {
        if (!string.IsNullOrWhiteSpace(context?.FechaFin))
        {
            return context!.FechaFin!.Trim();
        }

        if (!string.IsNullOrWhiteSpace(context?.Fecha))
        {
            var f = context!.Fecha!.Trim();
            if (f.StartsWith("AL ", StringComparison.OrdinalIgnoreCase))
            {
                return f[3..].Trim();
            }

            return f;
        }

        return DateTime.Today.ToString(CredixReportTokens.DateFormat, cult);
    }
}
