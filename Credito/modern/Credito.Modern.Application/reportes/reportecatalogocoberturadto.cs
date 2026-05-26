namespace Credito.Modern.Application.Reportes;

/// <summary>Resumen de sustitución API moderna por ítem del catálogo MVC.</summary>
public sealed record ReporteCatalogoCoberturaDto(
    int TotalCatalogo,
    int CompletoDatosJsonCsvPdf,
    int JsonSinExportTabular,
    int Parcial,
    int SoloMvc,
    int VistaIndice,
    IReadOnlyList<ReporteCatalogoCoberturaItemDto> Items,
    IReadOnlyList<ReporteCatalogoCoberturaItemDto> InformesAdicionalesApi,
    IReadOnlyList<ReporteCatalogoCoberturaItemDto> InformesTextoRdlc);

/// <summary>
/// Niveles: <c>completo-datos</c> (JSON+CSV+PDF tabular),
/// <c>json</c>, <c>json-texto</c>, <c>parcial</c>, <c>solo-mvc</c>, <c>vista-indice</c>.
/// </summary>
public sealed record ReporteCatalogoCoberturaItemDto(
    string CatalogoId,
    string Nombre,
    string Area,
    string AccionMvc,
    string NivelCobertura,
    string? JsonEndpoint,
    string? CsvEndpoint,
    string? PdfEndpoint,
    string Nota);
