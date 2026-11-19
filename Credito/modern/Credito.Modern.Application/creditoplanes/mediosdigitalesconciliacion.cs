using System.Globalization;
using System.Text.RegularExpressions;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Ingreso de un reporte externo (Yape Excel o captura Plin/Interbank).</summary>
public sealed record MedioDigitalReporteMovimiento(
    int Indice,
    string Canal,
    string Origen,
    decimal Monto,
    string Mensaje,
    DateTime FechaOperacion,
    string FechaOperacionRaw,
    string? FuenteArchivo = null);

public enum MedioDigitalMatchEstado
{
    Auto = 0,
    Sugerido = 1,
    Ambiguo = 2,
    SinMatch = 3,
}

public sealed record MedioDigitalMatchPairDto(
    int MovimientoCajaId,
    string? Cliente,
    string? Movimiento,
    decimal ImportePago,
    string? TipoPago,
    string? FechaTransferencia,
    string? Registro,
    int? ReporteIndice,
    string? ReporteCanal,
    string? ReporteOrigen,
    string? ReporteMensaje,
    string? ReporteFechaOperacion,
    decimal? ReporteMonto,
    string Estado,
    string Motivo);

public sealed record MedioDigitalConciliacionResultadoDto(
    string Fuente,
    int FilasArchivo,
    int FilasIngreso,
    int FilasDescartadas,
    decimal TotalIngresos,
    int PendientesSistema,
    int Auto,
    int Sugeridos,
    int Ambiguos,
    int SinMatch,
    int VerificadosAplicados,
    int YaVerificados,
    IReadOnlyList<MedioDigitalMatchPairDto> Pares,
    IReadOnlyList<string> Advertencias);

/// <summary>
/// Conciliación 1:1 determinística. AUTO solo con monto+minuto únicos.
/// Nombre es auxiliar (familiar / Plin-mario).
/// </summary>
public static class MedioDigitalConciliacionMatcher
{
    public static MedioDigitalConciliacionResultadoDto Match(
        string fuente,
        IReadOnlyList<MedioDigitalReporteMovimiento> ingresos,
        IReadOnlyList<PagosNoVerificadosRowDto> pendientes,
        int filasArchivo,
        int filasDescartadas,
        IReadOnlyList<string>? advertencias = null)
    {
        var pares = new List<MedioDigitalMatchPairDto>();
        var usedRep = new HashSet<int>();
        var usedMov = new HashSet<int>();

        static DateTime TruncMinute(DateTime dt) =>
            new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Unspecified);

        static long MoneyKey(decimal m) =>
            (long)Math.Round(m * 100m, MidpointRounding.AwayFromZero);

        static string MinuteKey(DateTime dt) =>
            TruncMinute(dt).ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);

        var repByBucket = ingresos
            .GroupBy(y => (MoneyKey(y.Monto), MinuteKey(y.FechaOperacion)))
            .ToDictionary(g => g.Key, g => g.ToList());

        var pendParsed = new List<(PagosNoVerificadosRowDto Row, DateTime? Minute, long Money)>();
        foreach (var p in pendientes)
        {
            DateTime? minute = null;
            if (YapeReporteParser.TryParseFecha(p.FechaTransferencia, out var ft))
            {
                minute = TruncMinute(ft);
            }

            pendParsed.Add((p, minute, MoneyKey(p.ImportePago)));
        }

        var pendByBucket = pendParsed
            .Where(p => p.Minute is not null)
            .GroupBy(p => (p.Money, MinuteKey(p.Minute!.Value)))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (bucket, reps) in repByBucket)
        {
            if (reps.Count != 1) continue;
            if (!pendByBucket.TryGetValue(bucket, out var pends) || pends.Count != 1) continue;

            var y = reps[0];
            var p = pends[0].Row;
            if (!usedRep.Add(y.Indice) || !usedMov.Add(p.MovimientoCajaId)) continue;

            pares.Add(ToPair(p, y, MedioDigitalMatchEstado.Auto,
                $"Monto y fecha/hora (minuto) únicos entre {fuente} y el sistema."));
        }

        foreach (var (bucket, reps) in repByBucket)
        {
            pendByBucket.TryGetValue(bucket, out var pends);
            pends ??= [];
            var freeY = reps.Where(y => !usedRep.Contains(y.Indice)).ToList();
            var freeP = pends.Where(p => !usedMov.Contains(p.Row.MovimientoCajaId)).ToList();
            if (freeY.Count == 0 || freeP.Count == 0) continue;
            if (freeY.Count == 1 && freeP.Count == 1) continue;

            var n = Math.Min(freeY.Count, freeP.Count);
            for (var i = 0; i < n; i++)
            {
                var y = freeY[i];
                var p = freeP[i].Row;
                usedRep.Add(y.Indice);
                usedMov.Add(p.MovimientoCajaId);
                pares.Add(ToPair(p, y, MedioDigitalMatchEstado.Ambiguo,
                    $"Hay {freeY.Count} ingreso(s) {fuente} y {freeP.Count} cobro(s) con el mismo monto y minuto; revisión manual."));
            }

            for (var i = n; i < freeP.Count; i++)
            {
                var p = freeP[i].Row;
                usedMov.Add(p.MovimientoCajaId);
                pares.Add(ToPair(p, null, MedioDigitalMatchEstado.Ambiguo,
                    $"Varios candidatos {fuente} con el mismo monto/minuto; revisión manual."));
            }
        }

        foreach (var (p, minute, money) in pendParsed)
        {
            if (usedMov.Contains(p.MovimientoCajaId)) continue;
            if (minute is null)
            {
                pares.Add(ToPair(p, null, MedioDigitalMatchEstado.SinMatch,
                    "El cobro no tiene fecha/hora de transferencia usable."));
                usedMov.Add(p.MovimientoCajaId);
                continue;
            }

            var candidates = ingresos
                .Where(y => !usedRep.Contains(y.Indice))
                .Where(y => MoneyKey(y.Monto) == money)
                .Select(y => (Y: y, Diff: Math.Abs((TruncMinute(y.FechaOperacion) - minute.Value).TotalMinutes)))
                .Where(x => x.Diff > 0 && x.Diff <= 1.0)
                .OrderBy(x => x.Diff)
                .ThenByDescending(x => NameScore(p.Cliente, x.Y.Origen, x.Y.Mensaje))
                .ToList();

            if (candidates.Count == 1)
            {
                var y = candidates[0].Y;
                usedRep.Add(y.Indice);
                usedMov.Add(p.MovimientoCajaId);
                pares.Add(ToPair(p, y, MedioDigitalMatchEstado.Sugerido,
                    "Monto exacto y hora cercana (±1 min); confirme manualmente (no es auto)."));
                continue;
            }

            if (candidates.Count > 1)
            {
                usedMov.Add(p.MovimientoCajaId);
                pares.Add(ToPair(p, candidates[0].Y, MedioDigitalMatchEstado.Ambiguo,
                    $"Varios ingresos {fuente} con el mismo monto en ±1 minuto."));
                continue;
            }

            var sameDay = ingresos
                .Where(y => !usedRep.Contains(y.Indice))
                .Where(y => MoneyKey(y.Monto) == money)
                .Where(y => TruncMinute(y.FechaOperacion).Date == minute.Value.Date)
                .Select(y => (Y: y, Score: NameScore(p.Cliente, y.Origen, y.Mensaje)))
                .Where(x => x.Score >= 40)
                .OrderByDescending(x => x.Score)
                .ToList();

            if (sameDay.Count == 1)
            {
                var y = sameDay[0].Y;
                usedRep.Add(y.Indice);
                usedMov.Add(p.MovimientoCajaId);
                pares.Add(ToPair(p, y, MedioDigitalMatchEstado.Sugerido,
                    "Monto+día y similitud de nombre/mensaje; confirme manualmente."));
                continue;
            }

            usedMov.Add(p.MovimientoCajaId);
            pares.Add(ToPair(p, null, MedioDigitalMatchEstado.SinMatch,
                $"No hay ingreso {fuente} único que coincida con monto y fecha/hora."));
        }

        return new MedioDigitalConciliacionResultadoDto(
            Fuente: fuente,
            FilasArchivo: filasArchivo,
            FilasIngreso: ingresos.Count,
            FilasDescartadas: filasDescartadas,
            TotalIngresos: ingresos.Sum(y => y.Monto),
            PendientesSistema: pendientes.Count,
            Auto: pares.Count(p => p.Estado == nameof(MedioDigitalMatchEstado.Auto)),
            Sugeridos: pares.Count(p => p.Estado == nameof(MedioDigitalMatchEstado.Sugerido)),
            Ambiguos: pares.Count(p => p.Estado == nameof(MedioDigitalMatchEstado.Ambiguo)),
            SinMatch: pares.Count(p => p.Estado == nameof(MedioDigitalMatchEstado.SinMatch)),
            VerificadosAplicados: 0,
            YaVerificados: 0,
            Pares: pares
                .OrderBy(p => p.Estado switch
                {
                    nameof(MedioDigitalMatchEstado.Auto) => 0,
                    nameof(MedioDigitalMatchEstado.Sugerido) => 1,
                    nameof(MedioDigitalMatchEstado.Ambiguo) => 2,
                    _ => 3,
                })
                .ThenBy(p => p.Cliente)
                .ToList(),
            Advertencias: advertencias ?? Array.Empty<string>());
    }

    public static bool EsTipoPagoCompatible(string? tipoPago, string canal)
    {
        var t = (tipoPago ?? string.Empty).Trim().ToUpperInvariant();
        t = t.Replace('Ó', 'O');
        return canal.Equals("Plin", StringComparison.OrdinalIgnoreCase)
            ? t.Contains("PLIN", StringComparison.Ordinal)
            : t.Contains("YAPE", StringComparison.Ordinal);
    }

    public static int NameScore(string? cliente, string? origen, string? mensaje)
    {
        var c = NormalizeName(cliente);
        if (c.Length < 2) return 0;

        var best = 0;
        foreach (var candidate in new[] { origen, mensaje })
        {
            var o = NormalizeName(candidate);
            if (o.Length < 2) continue;

            if (c.Contains(o, StringComparison.Ordinal) || o.Contains(c, StringComparison.Ordinal))
            {
                best = Math.Max(best, 80);
                continue;
            }

            var cTokens = c.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var oTokens = o.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (cTokens.Length == 0 || oTokens.Length == 0) continue;

            var hits = cTokens.Count(ct =>
                oTokens.Any(ot =>
                {
                    var clean = ot.TrimEnd('*');
                    return clean.Length >= 3 &&
                           (ct.StartsWith(clean, StringComparison.Ordinal) ||
                            ct.Contains(clean, StringComparison.Ordinal));
                }));

            best = Math.Max(best, (int)Math.Round(100.0 * hits / Math.Max(cTokens.Length, 1)));
        }

        return best;
    }

    private static MedioDigitalMatchPairDto ToPair(
        PagosNoVerificadosRowDto p,
        MedioDigitalReporteMovimiento? y,
        MedioDigitalMatchEstado estado,
        string motivo) =>
        new(
            p.MovimientoCajaId,
            p.Cliente,
            p.Movimiento,
            p.ImportePago,
            p.TipoPago,
            p.FechaTransferencia,
            p.Registro,
            y?.Indice,
            y?.Canal,
            y?.Origen,
            y?.Mensaje,
            y?.FechaOperacionRaw,
            y?.Monto,
            estado.ToString(),
            motivo);

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var s = value.Trim().ToUpperInvariant();
        s = s.Replace('Ó', 'O').Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ú', 'U')
            .Replace('Ñ', 'N');
        s = Regex.Replace(s, @"^(PLIN|BCP|BIM|BBVA|INTERBANK|YAPE)\s*[-:]\s*", "", RegexOptions.IgnoreCase);
        s = s.Replace("*", "", StringComparison.Ordinal);
        s = Regex.Replace(s, @"[^A-Z0-9\s]", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }
}
