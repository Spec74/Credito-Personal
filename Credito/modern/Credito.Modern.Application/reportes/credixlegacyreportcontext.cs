namespace Credito.Modern.Application.Reportes;

/// <summary>Metadatos de encabezado (paridad parámetros RDLC ReportParameter).</summary>
public sealed record CredixLegacyReportContext
{
    public string? Oficina { get; init; }
    public string? Agente { get; init; }
    public string? Gestor { get; init; }
    public string? Caja { get; init; }
    public string? Fecha { get; init; }
    public string? FechaIni { get; init; }
    public string? FechaFin { get; init; }
    public string? Estado { get; init; }
    public string? Titulo { get; init; }
    public string? NroClientes { get; init; }
    public string? SaldoVencido { get; init; }
    public string? SaldoMoroso { get; init; }
    public string? FechaReporte { get; init; }
    /// <summary>Referencia de consulta (crédito, persona, caja) cuando no hay filtros de oficina/periodo.</summary>
    public string? Referencia { get; init; }
}
