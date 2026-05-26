namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Contrato de sustitución de export (CSV tabular, JSON texto RDLC, motor RDLC pendiente).
/// </summary>
public sealed record ReporteExportPoliticaDto(
    string Fase,
    bool CsvTabularCompleto,
    string CatalogoModerno,
    string PoliticaEndpoint,
    IReadOnlyList<ReporteInformeTextoRdlcItemDto> InformesTextoRdlcSinCsv,
    IReadOnlyList<ReportePdfPilotoItemDto> InformesPdfPilotos,
    ReporteRdlcMotorEstadoDto RdlcMotor);

public sealed record ReportePdfPilotoItemDto(
    string Id,
    string JsonEndpoint,
    string CsvEndpoint,
    string PdfEndpoint,
    string Nota);

public sealed record ReporteInformeTextoRdlcItemDto(
    string Id,
    string JsonEndpoint,
    string Proc,
    string Nota);

public sealed record ReporteRdlcMotorEstadoDto(
    string Estado,
    string LegacyController,
    string CatalogoEndpoint,
    string ProximaRebanada);
