using System.Globalization;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.Reportes;
using Credito.Modern.Application.UsuariosAdmin;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// Encabezado profesional: nombre de oficina/gestor (no solo IDs) más periodo o fecha de corte.
    /// </summary>
    public static async Task<CredixLegacyReportContext> ResolveAsync(
        HttpContext http,
        int? oficinaId,
        int? usuarioId = null,
        DateTime? fechaIni = null,
        DateTime? fechaFin = null,
        DateTime? fecha = null,
        string? estado = null,
        string? titulo = null,
        int? anio = null,
        int? mes = null,
        string? caja = null,
        string? referencia = null,
        CancellationToken cancellationToken = default)
    {
        var oficinas = http.RequestServices.GetRequiredService<IOficinaReadService>();
        var usuarios = http.RequestServices.GetRequiredService<IUsuarioAdminReadService>();
        var cult = CultureInfo.CurrentCulture;

        CredixLegacyReportContext ctx;
        if (oficinaId is > 0)
        {
            ctx = await GestorInformePdfContextBuilder
                .BuildGestorOficinaAsync(oficinaId.Value, usuarioId, usuarios, oficinas, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            ctx = new CredixLegacyReportContext();
        }

        var periodo = anio is > 0 && mes is >= 1 and <= 12
            ? new DateTime(anio.Value, mes.Value, 1).ToString("MM/yyyy", cult)
            : null;

        return ctx with
        {
            FechaIni = FormatFecha(fechaIni) ?? ctx.FechaIni,
            FechaFin = FormatFecha(fechaFin) ?? ctx.FechaFin,
            Fecha = FormatFecha(fecha) ?? periodo ?? ctx.Fecha,
            Estado = estado ?? ctx.Estado,
            Titulo = titulo ?? ctx.Titulo,
            Caja = caja ?? ctx.Caja,
            Referencia = referencia ?? ctx.Referencia,
        };
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
