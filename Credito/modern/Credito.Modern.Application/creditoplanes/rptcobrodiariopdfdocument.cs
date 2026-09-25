using System.Globalization;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// PDF de cobro diario / morosidad por gestor — paridad visual RDLC
/// (<c>rptCobroDiario.rdlc</c>): orden por vencimiento, franjas VENCIDOS/VIGENTES
/// y resaltados en Último pago / Fecha vencimiento.
/// </summary>
public static class RptCobroDiarioPdfDocument
{
    private static readonly Color HighlightPagoSinReal = Color.FromHex("#7030A0");
    private static readonly Color HighlightPagoPeriodico = Color.FromHex("#28A745");
    private static readonly Color HighlightVencePronto = Color.FromHex("#FF6347"); // Tomato
    private static readonly Color GroupBannerBg = CredixReportTokens.Brand;

    public static byte[] Build(
        IReadOnlyList<RptCobroDiarioRowDto> rows,
        CredixLegacyReportContext context,
        bool soloMora)
    {
        var key = soloMora
            ? CredixLegacyReportKey.MorosidadGestor
            : CredixLegacyReportKey.CobroDiario;
        var def = CredixLegacyReportCatalog.Get(key);
        var title = string.IsNullOrWhiteSpace(context.Titulo) ? def.Title : context.Titulo.Trim();
        var meta = CredixLegacyReportCatalog.BuildMetadata(key, context with { Titulo = title });
        var corte = DateTime.Today;
        var ordered = OrderLikeLegacy(rows, corte);
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = CredixReportTokens.NowPrinted();
        var colCount = def.Columns.Count;
        var bodyFont = CredixLegacyPdfDocument.ResolveBodyFont(colCount);
        var (pageW, pageH) = CredixLegacyPdfDocument.ResolvePageSize(colCount, landscape: true);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(new PageSize(pageW, pageH));
                page.Margin(12);
                page.DefaultTextStyle(s => s.FontSize(bodyFont));

                page.Header().Column(col =>
                {
                    col.Item().Element(c =>
                        CredixLegacyPdfDocument.ComposeTitleBand(c, logo, title, printedAt));
                    if (meta.Count > 0)
                    {
                        col.Item().PaddingTop(4)
                            .Element(c => CredixLegacyPdfDocument.ComposeMetadataBlock(c, meta));
                    }
                });

                page.Content().PaddingTop(6).Table(table =>
                {
                    var weights = CredixLegacyPdfDocument.ComputeContentWeights(
                        def.Columns.Select(c => c.DisplayLabel).ToList(),
                        SampleDisplayRows(ordered, def),
                        def.Columns);
                    table.ColumnsDefinition(cols =>
                    {
                        for (var i = 0; i < colCount; i++)
                            cols.RelativeColumn(weights[i]);
                    });

                    table.Header(header =>
                    {
                        foreach (var col in def.Columns)
                        {
                            CredixPdfCellText.Write(
                                header.Cell().Element(HeaderCellFor(col.Align)),
                                col.DisplayLabel,
                                bodyFont,
                                bold: true,
                                color: Colors.White);
                        }
                    });

                    string? currentGroup = null;
                    foreach (var r in ordered)
                    {
                        var group = IsVencido(r, corte) ? "CRÉDITOS VENCIDOS" : "CRÉDITOS VIGENTES";
                        if (currentGroup != group)
                        {
                            currentGroup = group;
                            ComposeGroupBanner(table, colCount, group, bodyFont);
                        }

                        ComposeDataRow(table, def, r, corte, bodyFont);
                    }

                    if (def.TotalColumns.Count > 0)
                    {
                        var displayRows = SampleDisplayRows(ordered, def);
                        var totals = CredixReportTotals.Compute(def.Columns, displayRows, def.TotalColumns);
                        if (totals.Count > 0)
                        {
                            ComposeTotals(table, colCount, def, totals, bodyFont);
                        }
                    }
                });

                page.Footer().Element(c => CredixReportTokens.ComposeStandardFooter(c, ordered.Count));
            });
        }).GeneratePdf();
    }

    /// <summary>Paridad RDLC: vencidos primero, luego vigentes; dentro, por fecha de vencimiento.</summary>
    public static IReadOnlyList<RptCobroDiarioRowDto> OrderLikeLegacy(
        IReadOnlyList<RptCobroDiarioRowDto> rows,
        DateTime corte) =>
        rows
            .OrderBy(r => IsVencido(r, corte) ? 0 : 1)
            .ThenBy(r => r.FechaVencimiento)
            .ThenBy(r => r.Cliente ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.CreditoId)
            .ToList();

    public static bool IsVencido(RptCobroDiarioRowDto r, DateTime corte) =>
        r.FechaVencimiento.Date < corte.Date
        || (r.DiasAtrazo ?? 0) > 0;

    /// <summary>Paridad RDLC: morado sin pago real; verde si forma de pago periódica.</summary>
    public static Color? FechaPagoBackground(RptCobroDiarioRowDto r)
    {
        if (r.TienePagoReal == false)
            return HighlightPagoSinReal;

        var fp = (r.FormaPago ?? string.Empty).Trim().ToUpperInvariant();
        if (fp is "S" or "SEMANAL" or "Q" or "QUINCENAL" or "M" or "MENSUAL")
            return HighlightPagoPeriodico;

        return null;
    }

    /// <summary>Paridad RDLC: Tomato si vence hoy o en los próximos 3 días.</summary>
    public static Color? FechaVencimientoBackground(RptCobroDiarioRowDto r, DateTime corte)
    {
        var v = r.FechaVencimiento.Date;
        var hoy = corte.Date;
        if (v >= hoy && v <= hoy.AddDays(3))
            return HighlightVencePronto;
        return null;
    }

    private static void ComposeGroupBanner(
        TableDescriptor table,
        int colCount,
        string label,
        float fontSize)
    {
        table.Cell()
            .ColumnSpan((uint)colCount)
            .Background(GroupBannerBg)
            .Border(CredixLegacyPdfDocument.BodyBorderPt)
            .BorderColor(CredixReportTokens.Border)
            .PaddingVertical(3)
            .PaddingHorizontal(6)
            .Text($"{label}  « « « «")
            .FontColor(Colors.White)
            .Bold()
            .FontSize(fontSize);
    }

    private static void ComposeDataRow(
        TableDescriptor table,
        CredixLegacyReportDefinition def,
        RptCobroDiarioRowDto r,
        DateTime corte,
        float fontSize)
    {
        foreach (var col in def.Columns)
        {
            var raw = CellValue(r, col.CsvName);
            var display = CredixLegacyPdfDocument.FormatDisplayCell(raw, col);
            var bg = ResolveCellBackground(col.CsvName, r, corte);
            var fg = bg is null ? (Color?)null : Colors.White;
            var cell = table.Cell().Element(c => BodyCell(c, col.Align, bg));
            CredixPdfCellText.Write(cell, display, fontSize, bold: bg is not null, color: fg);
        }
    }

    private static Color? ResolveCellBackground(string csvName, RptCobroDiarioRowDto r, DateTime corte) =>
        csvName switch
        {
            "FechaPago" => FechaPagoBackground(r),
            "FechaVencimiento" => FechaVencimientoBackground(r, corte),
            _ => null,
        };

    private static string CellValue(RptCobroDiarioRowDto r, string csv) =>
        csv switch
        {
            "Nro" => r.Nro?.ToString(CredixReportTokens.Inv) ?? string.Empty,
            "Cliente" => r.Cliente ?? string.Empty,
            "MontoRecibido" => string.Empty,
            "Firma" => string.Empty,
            "Direccion" => r.Direccion ?? string.Empty,
            "Negocio" => r.Negocio ?? string.Empty,
            "Celular" => r.Celular ?? string.Empty,
            "TopeCredito" => r.TopeCredito?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "ClasificacionRiesgoSBS" => r.ClasificacionRiesgoSBS ?? string.Empty,
            "FechaPrimerPago" => ReportCsvFormats.FormatDateOrDateTime(r.FechaPrimerPago, CredixReportTokens.Inv),
            "NroCuotasPen" => r.NroCuotasPen?.ToString(CredixReportTokens.Inv) ?? string.Empty,
            "MontoCredito" => r.MontoCredito.ToString(CultureInfo.InvariantCulture),
            "Interes" => r.Interes.ToString(CultureInfo.InvariantCulture),
            "FormaPago" => r.FormaPago ?? string.Empty,
            "CuotaPlan" => r.CuotaPlan?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "MontoTotal" => r.MontoTotal?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "FechaPago" => ReportCsvFormats.FormatDateOrDateTime(r.FechaPago, CredixReportTokens.Inv),
            "Saldo" => r.Saldo?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "DiasAtrazo" => r.DiasAtrazo?.ToString(CredixReportTokens.Inv) ?? string.Empty,
            "FechaVencimiento" => ReportCsvFormats.FormatDateOrDateTime(r.FechaVencimiento, CredixReportTokens.Inv),
            "Mora" => r.Mora?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "CuotaTotal" => r.CuotaTotal?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            _ => string.Empty,
        };

    private static IReadOnlyList<IReadOnlyList<string>> SampleDisplayRows(
        IReadOnlyList<RptCobroDiarioRowDto> rows,
        CredixLegacyReportDefinition def) =>
        rows.Select(r => (IReadOnlyList<string>)def.Columns.Select(c => CellValue(r, c.CsvName)).ToList()).ToList();

    private static void ComposeTotals(
        TableDescriptor table,
        int colCount,
        CredixLegacyReportDefinition def,
        IReadOnlyDictionary<int, decimal> totals,
        float fontSize)
    {
        var firstTotal = totals.Keys.Min();
        if (firstTotal > 0)
        {
            table.Cell()
                .ColumnSpan((uint)firstTotal)
                .Element(TotalsCell)
                .AlignRight()
                .Element(c => CredixPdfCellText.Write(c, "TOTAL", fontSize, bold: true));
        }

        for (var c = firstTotal; c < colCount; c++)
        {
            var cell = table.Cell().Element(TotalsCell);
            if (totals.TryGetValue(c, out var total))
            {
                CredixPdfCellText.Write(
                    cell.AlignRight(),
                    CredixReportTotals.Format(total),
                    fontSize,
                    bold: true);
                continue;
            }

            CredixPdfCellText.Write(cell, string.Empty, fontSize);
        }
    }

    private static Func<IContainer, IContainer> HeaderCellFor(CredixColumnAlign align) =>
        align switch
        {
            CredixColumnAlign.Right => CredixLegacyPdfDocument.HeaderCellRight,
            CredixColumnAlign.Center => CredixLegacyPdfDocument.HeaderCellCenter,
            _ => CredixLegacyPdfDocument.HeaderCellLeft,
        };

    private static IContainer BodyCell(IContainer container, CredixColumnAlign align, Color? background)
    {
        var painted = background is null ? container : container.Background(background.Value);
        return align switch
        {
            CredixColumnAlign.Right => CredixLegacyPdfDocument.BodyCellNumeric(painted),
            CredixColumnAlign.Center => CredixLegacyPdfDocument.BodyCellCenter(painted),
            _ => CredixLegacyPdfDocument.BodyCell(painted),
        };
    }

    private static IContainer TotalsCell(IContainer container) =>
        container
            .Background(CredixReportTokens.TotalsBg)
            .Border(CredixLegacyPdfDocument.HeaderBorderPt)
            .BorderColor(CredixReportTokens.Border)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .AlignMiddle();
}
