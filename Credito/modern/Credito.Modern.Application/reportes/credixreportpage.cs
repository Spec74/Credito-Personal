using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Shell único de página para reportes PDF empresariales CrediConfiable.
/// <para>
/// Contrato: banda logo/razón social/título/fecha → meta SoftBg → cuerpo →
/// firmas opcionales → pie (empresa · página · filas).
/// </para>
/// <para>
/// Excepciones intencionales (no usar este shell):
/// tickets térmicos de caja, contratos/actas prendarias, plantilla comercial
/// del simulador de plan de pagos.
/// </para>
/// Seguridad: solo texto plano vía QuestPDF (sin HTML); logo desde
/// <see cref="CredixReportAssets"/> embebido.
/// </summary>
public static class CredixReportPage
{
    static CredixReportPage()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public sealed class Options
    {
        public bool Landscape { get; init; }
        /// <summary>Hoja con tamaño custom (p. ej. saldo cartera extra-ancho).</summary>
        public PageSize? PageSizeOverride { get; init; }
        public float? Margin { get; init; }
        public float? FontSize { get; init; }
        public bool Signatures { get; init; } = true;
        public int? FooterRowCount { get; init; }
        /// <summary>Columnas del bloque meta (2 o 3). Default 3.</summary>
        public int MetaColumns { get; init; } = 3;
    }

    /// <summary>
    /// Genera el PDF completo aplicando el chrome estándar.
    /// <paramref name="composeBody"/> solo dibuja el contenido de negocio
    /// (secciones/tablas); no debe repetir logo ni pie.
    /// </summary>
    public static byte[] Build(
        string title,
        IReadOnlyList<(string Label, string Value)> meta,
        Action<ColumnDescriptor> composeBody,
        Options? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(composeBody);
        options ??= new Options();

        var safeTitle = title.Trim();
        var safeMeta = SanitizeMeta(meta);
        var margin = options.Margin
            ?? (options.Landscape || options.PageSizeOverride is not null
                ? CredixReportTokens.PageMarginLandscapePt
                : CredixReportTokens.PageMarginPortraitPt);
        var fontSize = options.FontSize ?? CredixReportTokens.FontSizeBody;
        var pageSize = options.PageSizeOverride
            ?? (options.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(margin);
                page.DefaultTextStyle(s => s.FontSize(fontSize));

                page.Header().Element(c => ComposeHeader(c, safeTitle, safeMeta, options.MetaColumns));

                page.Content().PaddingTop(8).Column(col =>
                {
                    composeBody(col);
                    if (options.Signatures)
                    {
                        col.Item().PaddingTop(16).Element(CredixLegacyPdfDocument.ComposeSignatureBlock);
                    }
                });

                page.Footer().Element(c =>
                    CredixReportTokens.ComposeStandardFooter(c, options.FooterRowCount));
            });
        }).GeneratePdf();
    }

    /// <summary>Cabecera reutilizable (también para layouts custom que no usen <see cref="Build"/>).</summary>
    public static void ComposeHeader(
        IContainer container,
        string title,
        IReadOnlyList<(string Label, string Value)> meta,
        int metaColumns = 3)
    {
        var logo = CredixReportAssets.LoadLogo();
        var printedAt = CredixReportTokens.NowPrinted();
        var lines = SanitizeMeta(meta)
            .Select(f => new CredixLegacyPdfDocument.MetadataLine(f.Label, f.Value))
            .ToList();

        container.Column(col =>
        {
            col.Item().Element(c =>
                CredixLegacyPdfDocument.ComposeTitleBand(c, logo, title.Trim(), printedAt));
            if (lines.Count > 0)
            {
                col.Item().PaddingTop(6).Element(c => ComposeMeta(c, lines, metaColumns));
            }
        });
    }

    /// <summary>Solo el bloque meta SoftBg (sin banda de título).</summary>
    public static void ComposeMeta(
        IContainer container,
        IReadOnlyList<(string Label, string Value)> meta,
        int columns = 3)
    {
        var lines = SanitizeMeta(meta)
            .Select(f => new CredixLegacyPdfDocument.MetadataLine(f.Label, f.Value))
            .ToList();
        ComposeMeta(container, lines, columns);
    }

    private static void ComposeMeta(
        IContainer container,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> lines,
        int columns)
    {
        if (lines.Count == 0)
        {
            return;
        }

        if (columns == 2)
        {
            ComposeMetaTwoColumns(container, lines);
            return;
        }

        CredixLegacyPdfDocument.ComposeMetadataBlock(container, lines);
    }

    private static IReadOnlyList<(string Label, string Value)> SanitizeMeta(
        IReadOnlyList<(string Label, string Value)>? meta)
    {
        if (meta is null || meta.Count == 0)
        {
            return Array.Empty<(string, string)>();
        }

        // Solo texto plano; trim y descarta vacíos (evita basura / valores nulos en cabecera).
        return meta
            .Where(f => !string.IsNullOrWhiteSpace(f.Label) && !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => (f.Label.Trim(), f.Value.Trim()))
            .ToList();
    }

    private static void ComposeMetaTwoColumns(
        IContainer container,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> items)
    {
        container
            .Background(CredixReportTokens.SoftBg)
            .Border(0.6f)
            .BorderColor(CredixReportTokens.BorderSoft)
            .PaddingVertical(6)
            .PaddingHorizontal(8)
            .Column(col =>
            {
                for (var i = 0; i < items.Count; i += 2)
                {
                    var slice = items.Skip(i).Take(2).ToList();
                    col.Item().PaddingTop(i == 0 ? 0 : 2).Row(row =>
                    {
                        foreach (var line in slice)
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span($"{line.Label}: ").Bold().FontSize(CredixReportTokens.FontSizeMeta);
                                t.Span(line.Value).FontSize(CredixReportTokens.FontSizeMeta);
                            });
                        }

                        if (slice.Count == 1)
                        {
                            row.RelativeItem();
                        }
                    });
                }
            });
    }
}
