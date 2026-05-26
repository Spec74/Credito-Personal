using System.Globalization;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Api.Reportes;

/// <summary>Helpers para PDF legacy con metadatos (oficina, fechas) en endpoints.</summary>
public static class LegacyReportPdf
{
    public static async Task<string> ResolveOficinaAsync(
        IOficinaReadService oficinas,
        int oficinaId,
        CancellationToken ct)
    {
        var list = await oficinas.GetActivasAsync(ct).ConfigureAwait(false);
        return list.FirstOrDefault(o => o.OficinaId == oficinaId)?.Denominacion ?? "TODOS";
    }

    public static CredixLegacyReportContext Periodo(
        string oficina,
        DateTime? fechaIni = null,
        DateTime? fechaFin = null,
        string? gestor = null,
        string? agente = null,
        string? estado = null) =>
        new()
        {
            Oficina = oficina,
            FechaIni = FormatFecha(fechaIni),
            FechaFin = FormatFecha(fechaFin),
            Gestor = gestor,
            Agente = agente,
            Estado = estado,
        };

    public static CredixLegacyReportContext FechaOficina(
        string oficina,
        DateTime? fecha = null,
        string? agente = null) =>
        new()
        {
            Oficina = oficina,
            Fecha = FormatFecha(fecha ?? DateTime.Today),
            Agente = agente,
        };

    public static byte[] FromCsv(
        CredixLegacyReportKey key,
        byte[] csvUtf8Bom,
        CredixLegacyReportContext context) =>
        CredixLegacyPdfExports.FromCsv(key, csvUtf8Bom, context);

    public static byte[] FromTitle(
        string reportTitle,
        byte[] csvUtf8Bom,
        CredixLegacyReportContext? context = null) =>
        TabularPdfDocument.FromUtf8BomCsv(reportTitle, csvUtf8Bom, context: context);

    private static string? FormatFecha(DateTime? d) =>
        d?.ToString("d", CultureInfo.CurrentCulture);
}
