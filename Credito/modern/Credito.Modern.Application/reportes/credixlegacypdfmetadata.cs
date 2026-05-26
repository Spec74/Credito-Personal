namespace Credito.Modern.Application.Reportes;

/// <summary>Metadatos típicos de encabezado RDLC (Oficina, fechas, gestor).</summary>
public static class CredixLegacyPdfMetadata
{
    public static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> Oficina(string denominacion) =>
        [new("Oficina: ", denominacion)];

    public static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> OficinaYDiasAtrazo(
        string denominacion,
        int diasIni,
        int diasFin) =>
    [
        new("Oficina: ", denominacion),
        new("Dias atrazo del: ", $"{diasIni} al {diasFin}"),
    ];

    public static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> OficinaYGestor(
        string oficina,
        string gestor) =>
    [
        new("Oficina: ", oficina),
        new("Gestor: ", gestor),
    ];

    public static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> RangoFechas(
        string etiqueta,
        string desde,
        string hasta) =>
    [
        new($"{etiqueta} ", $"{desde} al {hasta}"),
    ];

    public static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> Merge(
        params IReadOnlyList<CredixLegacyPdfDocument.MetadataLine>[] blocks)
    {
        var list = new List<CredixLegacyPdfDocument.MetadataLine>();
        foreach (var block in blocks)
            list.AddRange(block);
        return list;
    }
}
