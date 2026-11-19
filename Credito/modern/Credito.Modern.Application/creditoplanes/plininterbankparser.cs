using System.Globalization;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.CreditoPlanes;

public sealed record PlinCapturaParseResult(
    IReadOnlyList<MedioDigitalReporteMovimiento> Ingresos,
    int FilasDetectadas,
    int FilasDescartadas,
    IReadOnlyList<string> Advertencias);

/// <summary>
/// Parsea texto OCR de capturas Interbank/Plin (movimientos tipo "Plin-mario", "Mar 17 Nov | 9:24 am", "S/ 500.00").
/// Solo ingresos Plin (prefijo Plin-). Filas Yape- se descartan en esta base.
/// </summary>
public static class PlinInterbankParser
{
    private static readonly Dictionary<string, int> Meses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENE"] = 1, ["ENERO"] = 1,
        ["FEB"] = 2, ["FEBRERO"] = 2,
        ["MAR"] = 3, ["MARZO"] = 3,
        ["ABR"] = 4, ["ABRIL"] = 4,
        ["MAY"] = 5, ["MAYO"] = 5,
        ["JUN"] = 6, ["JUNIO"] = 6,
        ["JUL"] = 7, ["JULIO"] = 7,
        ["AGO"] = 8, ["AGOSTO"] = 8,
        ["SET"] = 9, ["SEP"] = 9, ["SEPT"] = 9, ["SEPTIEMBRE"] = 9, ["SETIEMBRE"] = 9,
        ["OCT"] = 10, ["OCTUBRE"] = 10,
        ["NOV"] = 11, ["NOVIEMBRE"] = 11,
        ["DIC"] = 12, ["DICIEMBRE"] = 12,
    };

    // Plin-mario / Yape-elberto / PLIN - mario
    private static readonly Regex CanalOrigen = new(
        @"\b(?<canal>Plin|Yape)\s*[-–—:]\s*(?<origen>[A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9 .]{2,40})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Mar 17 Nov | 9:24 am   OR  17 Nov | 9:24 am
    private static readonly Regex FechaHora = new(
        @"(?:(?:Lun|Mar|Mi[eé]|Jue|Vie|S[aá]b|Dom)\s+)?(?<dia>\d{1,2})\s+(?<mes>Ene|Feb|Mar|Abr|May|Jun|Jul|Ago|Set|Sep|Sept|Oct|Nov|Dic)[a-z]*\s*[|¦]\s*(?<hora>\d{1,2}):(?<min>\d{2})\s*(?<ampm>a\.?\s*m\.?|p\.?\s*m\.?|am|pm)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // S/ 500.00  or S/500,00  (verde = ingreso; sin signo negativo)
    private static readonly Regex Monto = new(
        @"S/\s*(?<monto>\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})|\d+(?:[.,]\d{2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static PlinCapturaParseResult ParseOcrText(
        string ocrText,
        string? fuenteArchivo,
        DateTime? anioReferencia = null,
        bool soloPlin = true)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return new PlinCapturaParseResult([], 0, 0, ["OCR vacío o ilegible."]);
        }

        var year = (anioReferencia ?? DateTime.Now).Year;
        var normalized = ocrText
            .Replace('\u00A0', ' ')
            .Replace("—", "-")
            .Replace("–", "-");

        // Ventanas de ~4 líneas alrededor de cada canal detectado
        var lines = normalized
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var list = new List<MedioDigitalReporteMovimiento>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var detectadas = 0;
        var descartadas = 0;
        var idx = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            // Solo anclar en la línea del canal (evita re-leer la misma fila en ventanas).
            var canalMatch = CanalOrigen.Match(lines[i]);
            if (!canalMatch.Success) continue;

            detectadas++;
            var canal = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                canalMatch.Groups["canal"].Value.ToLowerInvariant());
            if (soloPlin && !canal.Equals("Plin", StringComparison.OrdinalIgnoreCase))
            {
                descartadas++;
                continue;
            }

            var origen = canalMatch.Groups["origen"].Value.Trim();
            // Buscar fecha/monto en esta línea y las 3 siguientes (layout Interbank).
            var block = string.Join("\n", lines.Skip(i).Take(4));
            var fechaMatch = FechaHora.Match(block);
            var montoMatch = Monto.Match(block);

            if (!fechaMatch.Success || !montoMatch.Success)
            {
                warnings.Add($"Captura {fuenteArchivo}: fila '{canal}-{origen}' sin fecha o monto legible.");
                descartadas++;
                continue;
            }

            if (!TryBuildFecha(fechaMatch, year, out var fecha, out var fechaRaw))
            {
                warnings.Add($"Captura {fuenteArchivo}: fecha ilegible cerca de '{origen}'.");
                descartadas++;
                continue;
            }

            if (!YapeReporteParser.TryParseMonto(montoMatch.Groups["monto"].Value, out var monto) || monto <= 0)
            {
                warnings.Add($"Captura {fuenteArchivo}: monto ilegible cerca de '{origen}'.");
                descartadas++;
                continue;
            }

            // Salidas típicas Interbank suelen ir en rojo; OCR a veces antepone "-"
            var around = block.ToUpperInvariant();
            if (around.Contains($"- S/ {montoMatch.Groups["monto"].Value}", StringComparison.Ordinal) ||
                around.Contains($"-{montoMatch.Value}", StringComparison.OrdinalIgnoreCase))
            {
                descartadas++;
                continue;
            }

            var key = $"{canal}|{monto:0.00}|{fecha:yyyyMMddHHmm}|{NormalizeKey(origen)}";
            if (!seen.Add(key))
            {
                descartadas++;
                continue; // solape entre capturas
            }

            idx++;
            list.Add(new MedioDigitalReporteMovimiento(
                Indice: idx,
                Canal: canal,
                Origen: origen,
                Monto: monto,
                Mensaje: string.Empty,
                FechaOperacion: fecha,
                FechaOperacionRaw: fechaRaw,
                FuenteArchivo: fuenteArchivo));
        }

        // Reindex global stable after merge will happen in service; local ok
        if (list.Count == 0)
        {
            warnings.Add($"Captura {fuenteArchivo}: no se extrajo ningún ingreso Plin usable.");
        }

        return new PlinCapturaParseResult(list, detectadas, descartadas, warnings);
    }

    public static PlinCapturaParseResult Merge(IEnumerable<PlinCapturaParseResult> parts)
    {
        var ingresos = new List<MedioDigitalReporteMovimiento>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var warns = new List<string>();
        var detectadas = 0;
        var descartadas = 0;
        var idx = 0;

        foreach (var part in parts)
        {
            detectadas += part.FilasDetectadas;
            descartadas += part.FilasDescartadas;
            warns.AddRange(part.Advertencias);
            foreach (var m in part.Ingresos)
            {
                var key = $"{m.Canal}|{m.Monto:0.00}|{m.FechaOperacion:yyyyMMddHHmm}|{NormalizeKey(m.Origen)}";
                if (!seen.Add(key))
                {
                    descartadas++;
                    continue;
                }

                idx++;
                ingresos.Add(m with { Indice = idx });
            }
        }

        return new PlinCapturaParseResult(ingresos, detectadas, descartadas, warns);
    }

    private static bool TryBuildFecha(Match m, int year, out DateTime fecha, out string raw)
    {
        fecha = default;
        raw = m.Value.Trim();
        if (!int.TryParse(m.Groups["dia"].Value, out var day)) return false;
        var mesKey = m.Groups["mes"].Value.Trim().ToUpperInvariant();
        mesKey = mesKey.Replace('Á', 'A').Replace('É', 'E');
        if (mesKey.Length > 3 && !Meses.ContainsKey(mesKey))
        {
            mesKey = mesKey[..3];
        }

        if (!Meses.TryGetValue(mesKey, out var month)) return false;
        if (!int.TryParse(m.Groups["hora"].Value, out var hour)) return false;
        if (!int.TryParse(m.Groups["min"].Value, out var minute)) return false;

        var ampm = m.Groups["ampm"].Value.Trim().ToLowerInvariant().Replace(".", "").Replace(" ", "");
        if (ampm.StartsWith('p') && hour < 12) hour += 12;
        if (ampm.StartsWith('a') && hour == 12) hour = 0;

        try
        {
            fecha = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            raw = fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeKey(string s) =>
        Regex.Replace(s.Trim().ToUpperInvariant(), @"\s+", " ");
}
