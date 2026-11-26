using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Parseo de constancias/pantallas de confirmación (foto al celular del cliente):
/// Plin, Yape, BCP, Interbank, Banco de la Nación.
/// </summary>
public static class VoucherConstanciaParser
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

    private static readonly Regex MontoRx = new(
        @"(?:S\s*/\s*|S\s+)\s*(?<m>\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?|\d+(?:[.,]\d{2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FechaTextoRx = new(
        @"(?<dia>\d{1,2})\s+(?<mes>Ene|Feb|Mar|Abr|May|Jun|Jul|Ago|Set|Sep|Sept|Oct|Nov|Dic)[a-z.]*\s+(?<anio>\d{4})(?:\s*[|•·\-–,]?\s*(?<hora>\d{1,2}):(?<min>\d{2})\s*(?<ampm>a\.?\s*m\.?|p\.?\s*m\.?|am|pm)?)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FechaLargaRx = new(
        @"(?:Lunes|Martes|Mi[eé]rcoles|Jueves|Viernes|S[aá]bado|Domingo)\s*,?\s*(?<dia>\d{1,2})\s+(?<mes>Enero|Febrero|Marzo|Abril|Mayo|Junio|Julio|Agosto|Setiembre|Septiembre|Octubre|Noviembre|Diciembre)\s+(?<anio>\d{4})\s*[-–]?\s*(?<hora>\d{1,2}):(?<min>\d{2})\s*(?<ampm>a\.?\s*m\.?|p\.?\s*m\.?|am|pm)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FechaSlashRx = new(
        @"(?<dia>\d{1,2})/(?<mes>\d{1,2})/(?<anio>\d{4})\s+(?<hora>\d{1,2}):(?<min>\d{2})(?::(?<seg>\d{2}))?",
        RegexOptions.Compiled);

    // Solo dígitos contiguos (no cruzar a la fecha de la línea siguiente).
    private static readonly Regex CodigoOpRx = new(
        @"(?:C[oó]digo\s+de\s+[Oo]peraci[oó]n|N(?:ro|úm(?:ero)?)\.?\s*(?:de\s+)?operaci[oó]n|N[uú]mero\s+de\s+operaci[oó]n)\s*[:\.]?\s*(?<cod>\d{5,24})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static VoucherConstanciaParseResult Parse(
        string ocrText,
        decimal? montoEsperado = null,
        DateTime? ahora = null,
        string? canalEsperado = null)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            return Fail(["OCR vacío o ilegible. Acerque la cámara a la constancia."]);
        }

        var text = Normalize(ocrText);
        if (text.Length < 12)
        {
            return Fail(["Texto OCR demasiado corto. Repita la foto más cerca y enfocada."]);
        }

        var canal = DetectCanal(text);
        if (canal is null)
        {
            warnings.Add("No se reconoció Plin, Yape, BCP, Interbank ni Banco de la Nación.");
        }
        else if (!string.IsNullOrWhiteSpace(canalEsperado) &&
                 !CanalesCompatibles(canal, canalEsperado))
        {
            warnings.Add(
                $"Constancia parece {canal}, pero el medio del cobro es {canalEsperado}. Verifique.");
        }

        if (text.Contains("DIFERIDA", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("DE FORMA DIFERIDA", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Transferencia diferida: el abono puede no ser inmediato.");
        }

        if (!TryExtractMonto(text, montoEsperado, out var monto, out var montoAmbiguo))
        {
            return Fail(["No se leyó el monto (S/ …). Enfoque la cifra grande del voucher."], canal);
        }

        if (montoAmbiguo)
        {
            warnings.Add("Había varios montos en la constancia; se eligió el más cercano al cobro.");
        }

        if (!TryExtractFecha(text, out var fecha, out var fechaRaw, out var tieneHora))
        {
            return Fail(
                ["No se leyó fecha/hora del voucher. Pruebe otra foto más nítida."],
                canal,
                monto);
        }

        if (!tieneHora)
        {
            warnings.Add("Solo se leyó la fecha (sin hora). Complete la hora manualmente si hace falta.");
        }

        var codigo = ExtractCodigo(text);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            warnings.Add("No se leyó el código/número de operación (opcional).");
        }

        var coincideMonto = true;
        if (montoEsperado is > 0)
        {
            var esp = decimal.Round(montoEsperado.Value, 2, MidpointRounding.AwayFromZero);
            coincideMonto = esp == monto;
            if (!coincideMonto)
            {
                warnings.Add(
                    $"Monto del voucher (S/ {monto:0.00}) no coincide con el cobro (S/ {esp:0.00}).");
            }
        }

        var now = ahora ?? DateTime.Now;
        var diffMin = Math.Abs((now - fecha).TotalMinutes);
        if (fecha > now.AddMinutes(30))
        {
            warnings.Add($"La hora del voucher ({fechaRaw}) está en el futuro respecto al reloj de caja.");
        }
        else if (diffMin > 24 * 60)
        {
            warnings.Add(
                $"La hora del voucher ({fechaRaw}) difiere más de 24 h del reloj de caja.");
        }
        else if (diffMin > 180)
        {
            warnings.Add(
                $"La hora del voucher ({fechaRaw}) difiere más de 3 h del reloj de caja.");
        }

        return new VoucherConstanciaParseResult(
            Ok: true,
            CanalDetectado: canal,
            Monto: monto,
            FechaOperacion: fecha,
            FechaOperacionRaw: fechaRaw,
            CodigoOperacion: codigo,
            CoincideMonto: coincideMonto,
            Advertencias: warnings,
            TextoOcrPreview: Truncate(text, 400));
    }

    /// <summary>Mapea TipoPago / marca UI a canal de constancia.</summary>
    public static string? CanalDesdeMarcaOTipo(string? marcaOrTipoPagoLabel)
    {
        if (string.IsNullOrWhiteSpace(marcaOrTipoPagoLabel)) return null;
        var u = Fold(marcaOrTipoPagoLabel);
        if (u.Contains("YAPE", StringComparison.Ordinal)) return "Yape";
        if (u.Contains("PLIN", StringComparison.Ordinal)) return "Plin";
        if (u.Contains("INTERBANK", StringComparison.Ordinal)) return "Interbank";
        if (u.Contains("NACION", StringComparison.Ordinal) || u.Contains("NACIÓN", StringComparison.Ordinal))
            return "BancoNacion";
        if (u.Contains("BCP", StringComparison.Ordinal) || u.Contains("CREDITO", StringComparison.Ordinal))
            return "BCP";
        return null;
    }

    private static bool CanalesCompatibles(string detectado, string esperado)
    {
        var a = Fold(detectado);
        var b = Fold(esperado);
        if (a == b) return true;
        // Plineo Interbank ↔ Plin / Interbank
        if ((a == "PLIN" && b == "INTERBANK") || (a == "INTERBANK" && b == "PLIN"))
            return true;
        return false;
    }

    private static VoucherConstanciaParseResult Fail(
        IReadOnlyList<string> warnings,
        string? canal = null,
        decimal? monto = null) =>
        new(
            Ok: false,
            CanalDetectado: canal,
            Monto: monto,
            FechaOperacion: null,
            FechaOperacionRaw: null,
            CodigoOperacion: null,
            CoincideMonto: false,
            Advertencias: warnings,
            TextoOcrPreview: null);

    private static string Normalize(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (ch is '\u00A0' or '\u2007' or '\u202F')
            {
                sb.Append(' ');
                continue;
            }

            sb.Append(ch switch
            {
                '—' or '–' or '•' or '·' or '|' or '¦' => ' ',
                _ => ch,
            });
        }

        var s = sb.ToString();
        // Ruido OCR frecuente en constancias peruanas
        s = Regex.Replace(s, @"\bS\s*1\s*/", "S/", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\b5/\s*", "S/", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"[ \t]{2,}", " ");
        return s;
    }

    private static string Fold(string s) =>
        s.Trim().ToUpperInvariant()
            .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I')
            .Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ñ', 'N');

    private static string? DetectCanal(string text)
    {
        var u = Fold(text);
        if (u.Contains("TE YAPEARON", StringComparison.Ordinal) ||
            u.Contains("NUEVO YAPEO", StringComparison.Ordinal) ||
            (u.Contains("YAPE", StringComparison.Ordinal) &&
             (u.Contains("NRO DE OPERACION", StringComparison.Ordinal) ||
              u.Contains("CODIGO DE SEGURIDAD", StringComparison.Ordinal))))
        {
            return "Yape";
        }

        if (u.Contains("PLINEO EXITOSO", StringComparison.Ordinal) ||
            (u.Contains("PLIN", StringComparison.Ordinal) &&
             (u.Contains("PAGO EXITOSO", StringComparison.Ordinal) ||
              u.Contains("CONSTANCIA", StringComparison.Ordinal))))
        {
            return "Plin";
        }

        if (u.Contains("BANCO DE LA NACION", StringComparison.Ordinal) ||
            (u.Contains("ENTIDAD DESTINO", StringComparison.Ordinal) &&
             u.Contains("NRO OPERACION", StringComparison.Ordinal)))
        {
            return "BancoNacion";
        }

        if (u.Contains("INTERBANK", StringComparison.Ordinal) ||
            u.Contains("TRANSFERISTE", StringComparison.Ordinal) ||
            u.Contains("PAGASTE", StringComparison.Ordinal))
        {
            return "Interbank";
        }

        if (u.Contains("TRANSFERENCIA EXITOSA", StringComparison.Ordinal) ||
            u.Contains("BANCO DE CREDITO", StringComparison.Ordinal) ||
            Regex.IsMatch(u, @"\bBCP\b"))
        {
            return "BCP";
        }

        if (u.Contains("PLIN", StringComparison.Ordinal)) return "Plin";
        if (u.Contains("YAPE", StringComparison.Ordinal)) return "Yape";
        return null;
    }

    private static bool TryExtractMonto(
        string text,
        decimal? montoEsperado,
        out decimal monto,
        out bool ambiguo)
    {
        monto = 0m;
        ambiguo = false;
        var candidates = new List<decimal>();
        foreach (Match m in MontoRx.Matches(text))
        {
            if (!YapeReporteParser.TryParseMonto(m.Groups["m"].Value, out var v) || v <= 0)
            {
                continue;
            }

            // Ignorar comisiones típicas muy chicas si hay montos mayores (S/ 0 / Gratis no entra).
            if (v < 0.01m) continue;
            if (!candidates.Contains(v))
            {
                candidates.Add(v);
            }
        }

        if (candidates.Count == 0) return false;

        if (montoEsperado is > 0)
        {
            var esp = decimal.Round(montoEsperado.Value, 2, MidpointRounding.AwayFromZero);
            var exact = candidates.FirstOrDefault(c => c == esp);
            if (exact > 0)
            {
                monto = exact;
                ambiguo = candidates.Count > 1;
                return true;
            }

            // Tolerancia 1 céntimo por redondeo OCR
            var near = candidates
                .Select(c => (c, d: Math.Abs(c - esp)))
                .OrderBy(x => x.d)
                .First();
            if (near.d <= 0.05m)
            {
                monto = near.c;
                ambiguo = candidates.Count > 1;
                return true;
            }
        }

        // Heurística: preferir montos "hero" (no el cargo=monto+comisión si hay esperado fallido).
        // Sin esperado: el mayor suele ser el del voucher (salvo BN con cargo total).
        // Si hay "MONTO PAGADO" / "MONTO CARGADO", preferir el de esa zona.
        var labeled = TryMontoCercaEtiqueta(text, "MONTO PAGADO")
            ?? TryMontoCercaEtiqueta(text, "PAGO EXITOSO")
            ?? TryMontoCercaEtiqueta(text, "TE YAPEARON")
            ?? TryMontoCercaEtiqueta(text, "TRANSFERISTE")
            ?? TryMontoCercaEtiqueta(text, "PAGASTE")
            ?? TryMontoCercaEtiqueta(text, "PLINEO");
        if (labeled is > 0)
        {
            monto = labeled.Value;
            ambiguo = candidates.Count > 1;
            return true;
        }

        monto = candidates.Max();
        ambiguo = candidates.Count > 1;
        return true;
    }

    private static decimal? TryMontoCercaEtiqueta(string text, string etiqueta)
    {
        var idx = text.IndexOf(etiqueta, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = Math.Max(0, idx - 40);
        var len = Math.Min(text.Length - start, 120);
        var slice = text.Substring(start, len);
        var best = 0m;
        var found = false;
        foreach (Match m in MontoRx.Matches(slice))
        {
            if (!YapeReporteParser.TryParseMonto(m.Groups["m"].Value, out var v) || v <= 0) continue;
            if (!found || v > best)
            {
                best = v;
                found = true;
            }
        }

        return found ? best : null;
    }

    private static bool TryExtractFecha(
        string text,
        out DateTime fecha,
        out string raw,
        out bool tieneHora)
    {
        fecha = default;
        raw = "";
        tieneHora = false;

        var mLarga = FechaLargaRx.Match(text);
        if (mLarga.Success && TryBuildFechaNamed(mLarga, out fecha, out raw, out tieneHora))
        {
            return true;
        }

        // Preferir match que traiga hora
        DateTime? best = null;
        string? bestRaw = null;
        var bestHasTime = false;
        foreach (Match mTexto in FechaTextoRx.Matches(text))
        {
            if (!TryBuildFechaNamed(mTexto, out var f, out var r, out var ht)) continue;
            if (best is null || (ht && !bestHasTime))
            {
                best = f;
                bestRaw = r;
                bestHasTime = ht;
            }
        }

        if (best is not null)
        {
            fecha = best.Value;
            raw = bestRaw!;
            tieneHora = bestHasTime;
            return true;
        }

        var mSlash = FechaSlashRx.Match(text);
        if (mSlash.Success)
        {
            if (!int.TryParse(mSlash.Groups["dia"].Value, out var d) ||
                !int.TryParse(mSlash.Groups["mes"].Value, out var mo) ||
                !int.TryParse(mSlash.Groups["anio"].Value, out var y) ||
                !int.TryParse(mSlash.Groups["hora"].Value, out var h) ||
                !int.TryParse(mSlash.Groups["min"].Value, out var mi))
            {
                return false;
            }

            var seg = 0;
            _ = int.TryParse(mSlash.Groups["seg"].Value, out seg);
            try
            {
                fecha = new DateTime(y, mo, d, h, mi, seg, DateTimeKind.Unspecified);
                raw = fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                tieneHora = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        var soloFecha = Regex.Match(
            text,
            @"(?<dia>\d{1,2})\s+(?<mes>Ene|Feb|Mar|Abr|May|Jun|Jul|Ago|Set|Sep|Sept|Oct|Nov|Dic)[a-z.]*\s+(?<anio>\d{4})",
            RegexOptions.IgnoreCase);
        var soloHora = Regex.Match(
            text,
            @"(?<hora>\d{1,2}):(?<min>\d{2})\s*(?<ampm>a\.?\s*m\.?|p\.?\s*m\.?|am|pm)",
            RegexOptions.IgnoreCase);
        if (soloFecha.Success && soloHora.Success)
        {
            var combined = $"{soloFecha.Value} {soloHora.Value}";
            var fake = FechaTextoRx.Match(combined);
            if (fake.Success && TryBuildFechaNamed(fake, out fecha, out raw, out tieneHora))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildFechaNamed(
        Match m,
        out DateTime fecha,
        out string raw,
        out bool tieneHora)
    {
        fecha = default;
        raw = m.Value.Trim();
        tieneHora = false;
        if (!int.TryParse(m.Groups["dia"].Value, out var day)) return false;
        if (!int.TryParse(m.Groups["anio"].Value, out var year)) return false;
        if (year is < 2000 or > 2100) return false;

        var mesKey = m.Groups["mes"].Value.Trim().ToUpperInvariant()
            .Replace('Á', 'A').Replace('É', 'E').Replace(".", "", StringComparison.Ordinal);
        if (mesKey.Length > 3 && !Meses.ContainsKey(mesKey))
        {
            mesKey = mesKey[..Math.Min(3, mesKey.Length)];
        }

        if (!Meses.TryGetValue(mesKey, out var month) && mesKey.Length >= 3)
        {
            Meses.TryGetValue(mesKey[..3], out month);
        }

        if (month < 1) return false;

        var hour = 0;
        var minute = 0;
        if (m.Groups["hora"].Success && !string.IsNullOrEmpty(m.Groups["hora"].Value) &&
            m.Groups["min"].Success && !string.IsNullOrEmpty(m.Groups["min"].Value))
        {
            _ = int.TryParse(m.Groups["hora"].Value, out hour);
            _ = int.TryParse(m.Groups["min"].Value, out minute);
            var ampm = m.Groups["ampm"].Success
                ? m.Groups["ampm"].Value.Trim().ToLowerInvariant().Replace(".", "").Replace(" ", "")
                : "";
            if (ampm.StartsWith('p') && hour < 12) hour += 12;
            if (ampm.StartsWith('a') && hour == 12) hour = 0;
            tieneHora = true;
        }

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

    private static string? ExtractCodigo(string text)
    {
        var m = CodigoOpRx.Match(text);
        if (m.Success)
        {
            return m.Groups["cod"].Value;
        }

        // Misma línea / hasta el salto: evita engullir el día de la fecha (0007764 + 26/04…).
        var idx = text.IndexOf("operaci", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var end = text.IndexOfAny(['\r', '\n'], idx);
            var slice = end < 0 ? text[idx..] : text[idx..end];
            var f = Regex.Match(slice, @"\b(?<cod>\d{6,20})\b");
            if (f.Success) return f.Groups["cod"].Value;
        }

        return null;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

public sealed record VoucherConstanciaParseResult(
    bool Ok,
    string? CanalDetectado,
    decimal? Monto,
    DateTime? FechaOperacion,
    string? FechaOperacionRaw,
    string? CodigoOperacion,
    bool CoincideMonto,
    IReadOnlyList<string> Advertencias,
    string? TextoOcrPreview);
