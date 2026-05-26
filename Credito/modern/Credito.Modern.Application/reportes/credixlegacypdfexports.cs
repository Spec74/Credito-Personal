namespace Credito.Modern.Application.Reportes;

/// <summary>Punto de entrada para PDF con layout legacy desde CSV o claves de informe.</summary>
public static class CredixLegacyPdfExports
{
    public static byte[] FromCsv(
        CredixLegacyReportKey key,
        byte[] csvUtf8Bom,
        CredixLegacyReportContext? context = null,
        IReadOnlyList<CredixLegacyPdfDocument.MetadataLine>? metadata = null)
    {
        var ctx = context ?? new CredixLegacyReportContext();
        var meta = metadata ?? CredixLegacyReportCatalog.BuildMetadata(key, ctx);
        return CredixLegacyCsvPdfDocument.Build(key, csvUtf8Bom, meta, ctx);
    }
}
