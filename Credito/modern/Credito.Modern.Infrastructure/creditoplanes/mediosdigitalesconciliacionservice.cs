using Credito.Modern.Application.CreditoPlanes;
using Microsoft.Extensions.Logging;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MediosDigitalesConciliacionService(
    IPagosNoVerificadosReadService pagosNoVerificados,
    IVerificarPagoTransferenciaWriteService verificarPago,
    IOcrEngine ocr,
    ILogger<MediosDigitalesConciliacionService> logger) : IMediosDigitalesConciliacionService
{
    public async Task<MedioDigitalConciliacionResultadoDto> ConciliarYapeAsync(
        int oficinaId,
        Stream archivo,
        string fileName,
        bool aplicarAutos,
        CancellationToken cancellationToken = default)
    {
        EnsureOficina(oficinaId);
        ArgumentNullException.ThrowIfNull(archivo);

        var parsed = YapeReporteParser.Parse(archivo, fileName);
        var pendientes = await LoadPendientesAsync(oficinaId, "Yape", cancellationToken)
            .ConfigureAwait(false);
        var adv = parsed.Advertencias.ToList();
        if (pendientes.Count == 0)
        {
            adv.Add("No hay pagos Yape pendientes de verificar en esta oficina (caja abierta).");
        }

        var resultado = MedioDigitalConciliacionMatcher.Match(
            "Yape",
            parsed.Ingresos,
            pendientes,
            parsed.FilasArchivo,
            parsed.FilasPagasteDescartadas,
            adv);

        return await MaybeApplyAsync(resultado, aplicarAutos, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MedioDigitalConciliacionResultadoDto> ConciliarPlinCapturasAsync(
        int oficinaId,
        IReadOnlyList<(Stream Stream, string FileName)> capturas,
        bool aplicarAutos,
        CancellationToken cancellationToken = default)
    {
        EnsureOficina(oficinaId);
        if (capturas is null || capturas.Count == 0)
        {
            throw new InvalidOperationException("Suba al menos una captura de movimientos Plin/Interbank.");
        }

        if (!ocr.IsAvailable)
        {
            throw new InvalidOperationException(
                "OCR no está disponible en este servidor. En Docker debe instalarse tesseract-ocr (spa).");
        }

        if (capturas.Count > 30)
        {
            throw new InvalidOperationException("Máximo 30 capturas por lote.");
        }

        var parts = new List<PlinCapturaParseResult>();
        var yearHint = DateTime.Now;
        foreach (var (stream, name) in capturas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = await ocr.ExtractTextAsync(stream, name, cancellationToken).ConfigureAwait(false);
            parts.Add(PlinInterbankParser.ParseOcrText(text, name, yearHint, soloPlin: true));
        }

        var merged = PlinInterbankParser.Merge(parts);
        var pendientes = await LoadPendientesAsync(oficinaId, "Plin", cancellationToken)
            .ConfigureAwait(false);
        var adv = merged.Advertencias.ToList();
        if (pendientes.Count == 0)
        {
            adv.Add("No hay pagos Plin pendientes de verificar en esta oficina (caja abierta).");
        }

        var resultado = MedioDigitalConciliacionMatcher.Match(
            "Plin",
            merged.Ingresos,
            pendientes,
            merged.FilasDetectadas,
            merged.FilasDescartadas,
            adv);

        return await MaybeApplyAsync(resultado, aplicarAutos, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PagosNoVerificadosRowDto>> LoadPendientesAsync(
        int oficinaId,
        string canal,
        CancellationToken ct)
    {
        var all = await pagosNoVerificados.ListarAsync(oficinaId, ct).ConfigureAwait(false);
        return all
            .Where(p => MedioDigitalConciliacionMatcher.EsTipoPagoCompatible(p.TipoPago, canal))
            .ToList();
    }

    private async Task<MedioDigitalConciliacionResultadoDto> MaybeApplyAsync(
        MedioDigitalConciliacionResultadoDto resultado,
        bool aplicarAutos,
        CancellationToken ct)
    {
        if (!aplicarAutos)
        {
            return resultado;
        }

        var autos = resultado.Pares
            .Where(p => string.Equals(p.Estado, nameof(MedioDigitalMatchEstado.Auto), StringComparison.Ordinal))
            .Select(p => p.MovimientoCajaId)
            .Distinct()
            .ToList();

        if (autos.Count == 0)
        {
            return resultado with
            {
                Advertencias = resultado.Advertencias
                    .Append("No hay coincidencias únicas (AUTO) para verificar automáticamente.")
                    .ToList(),
            };
        }

        var aplicados = 0;
        var ya = 0;
        var errores = new List<string>();
        foreach (var movId in autos)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var r = await verificarPago.VerificarAsync(movId, ct).ConfigureAwait(false);
                if (r.YaVerificado) ya++;
                else aplicados++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Auto-verify falló MovimientoCajaId={Id}", movId);
                errores.Add($"Mov. {movId}: no se pudo verificar.");
            }
        }

        var adv = resultado.Advertencias.ToList();
        adv.AddRange(errores);
        if (aplicados + ya > 0)
        {
            adv.Add($"Auto-verificados: {aplicados} nuevos, {ya} ya estaban verificados.");
        }

        return resultado with
        {
            VerificadosAplicados = aplicados,
            YaVerificados = ya,
            Advertencias = adv,
        };
    }

    private static void EnsureOficina(int oficinaId)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }
    }
}
