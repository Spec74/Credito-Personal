using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Credito.Modern.Application.CreditoPlanes;

public sealed record YapeReporteParseResult(
    IReadOnlyList<MedioDigitalReporteMovimiento> Ingresos,
    int FilasArchivo,
    int FilasPagasteDescartadas,
    IReadOnlyList<string> Advertencias);

/// <summary>Parser de reporte Yape (CSV / XLSX). Solo TE PAGÓ.</summary>
public static class YapeReporteParser
{
    private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");
    private static readonly string[] FormatosFecha =
    [
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy H:mm:ss",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy H:mm",
        "d/M/yyyy HH:mm:ss",
        "d/M/yyyy HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-ddTHH:mm",
    ];

    public static YapeReporteParseResult Parse(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var name = (fileName ?? string.Empty).Trim();
        var ext = Path.GetExtension(name).ToLowerInvariant();

        if (ext is ".xlsx" or ".xlsm")
        {
            return ParseXlsx(stream);
        }

        if (ext is ".csv" or ".txt" or "")
        {
            return ParseCsv(stream);
        }

        if (stream.CanSeek)
        {
            var pos = stream.Position;
            try
            {
                return ParseXlsx(stream);
            }
            catch
            {
                stream.Position = pos;
                return ParseCsv(stream);
            }
        }

        throw new InvalidOperationException(
            "Formato no soportado. Suba el reporte Yape en Excel (.xlsx) o CSV.");
    }

    public static bool EsTePago(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return false;
        var n = NormalizeHeader(tipo);
        return n.Contains("TE PAGO", StringComparison.Ordinal);
    }

    public static bool EsPagaste(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return false;
        var n = NormalizeHeader(tipo);
        return n.Contains("PAGASTE", StringComparison.Ordinal);
    }

    private static YapeReporteParseResult ParseXlsx(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var used = ws.RangeUsed();
        if (used is null)
        {
            return new YapeReporteParseResult([], 0, 0, ["El Excel no tiene filas."]);
        }

        var rows = used.RowsUsed().ToList();
        if (rows.Count == 0)
        {
            return new YapeReporteParseResult([], 0, 0, ["El Excel no tiene filas."]);
        }

        var matrix = rows
            .Select(r => r.Cells().Select(CellText).ToList())
            .ToList();

        return ParseMatrix(matrix);
    }

    private static YapeReporteParseResult ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new YapeReporteParseResult([], 0, 0, ["El CSV está vacío."]);
        }

        var lines = text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        if (lines.Count == 0)
        {
            return new YapeReporteParseResult([], 0, 0, ["El CSV está vacío."]);
        }

        var sep = DetectSeparator(lines);
        var matrix = lines.Select(l => SplitCsvLine(l, sep)).ToList();
        return ParseMatrix(matrix);
    }

    private static char DetectSeparator(IReadOnlyList<string> lines)
    {
        var sample = lines.Take(8).ToList();
        var semi = sample.Sum(l => l.Count(c => c == ';'));
        var comma = sample.Sum(l => l.Count(c => c == ','));
        var tab = sample.Sum(l => l.Count(c => c == '\t'));
        if (tab >= semi && tab >= comma && tab > 0) return '\t';
        if (semi >= comma) return ';';
        return ',';
    }

    private static List<string> SplitCsvLine(string line, char sep)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == sep && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        result.Add(sb.ToString().Trim());
        return result;
    }

    private static YapeReporteParseResult ParseMatrix(List<List<string>> matrix)
    {
        if (matrix.Count == 0)
        {
            return new YapeReporteParseResult([], 0, 0, ["Sin filas en el archivo."]);
        }

        var headerIdx = FindHeaderRow(matrix);
        ColumnMap map;
        int dataStart;
        var warnings = new List<string>();
        if (headerIdx >= 0)
        {
            map = MapColumns(matrix[headerIdx]);
            dataStart = headerIdx + 1;
        }
        else
        {
            map = ColumnMap.DefaultPositional();
            dataStart = FindFirstDataRow(matrix);
            if (dataStart < 0)
            {
                return new YapeReporteParseResult([], 0, 0,
                    ["No se encontraron filas TE PAGÓ / PAGASTE en el archivo."]);
            }

            warnings.Add("No se detectó encabezado; se asumió orden Tipo, Origen, Monto, Mensaje, Fecha.");
        }

        if (!map.IsValid)
        {
            throw new InvalidOperationException(
                "No se reconocieron columnas del reporte Yape (se esperan Tipo, Origen/Destinatario, Monto y Fecha).");
        }

        var list = new List<MedioDigitalReporteMovimiento>();
        var idx = 0;
        var pagaste = 0;
        var filasDatos = 0;
        for (var r = dataStart; r < matrix.Count; r++)
        {
            var row = matrix[r];
            var tipo = Get(row, map.Tipo);
            if (string.IsNullOrWhiteSpace(tipo)) continue;
            if (LooksLikeTotal(tipo) || LooksLikeTotal(Get(row, map.Monto))) continue;

            filasDatos++;
            if (EsPagaste(tipo))
            {
                pagaste++;
                continue;
            }

            if (!EsTePago(tipo)) continue;

            if (!TryParseMonto(Get(row, map.Monto), out var monto) || monto <= 0m)
            {
                warnings.Add($"Fila {r + 1}: TE PAGÓ sin monto válido.");
                continue;
            }

            var fechaRaw = Get(row, map.Fecha);
            if (!TryParseFecha(fechaRaw, out var fecha))
            {
                warnings.Add($"Fila {r + 1}: TE PAGÓ sin fecha/hora válida ({fechaRaw}).");
                continue;
            }

            idx++;
            list.Add(new MedioDigitalReporteMovimiento(
                Indice: idx,
                Canal: "Yape",
                Origen: Get(row, map.Origen).Trim(),
                Monto: decimal.Round(monto, 2, MidpointRounding.AwayFromZero),
                Mensaje: Get(row, map.Mensaje).Trim(),
                FechaOperacion: fecha,
                FechaOperacionRaw: fechaRaw.Trim()));
        }

        if (list.Count == 0)
        {
            warnings.Add("No quedó ningún TE PAGÓ usable tras filtrar PAGASTE y validar monto/fecha.");
        }

        return new YapeReporteParseResult(list, filasDatos, pagaste, warnings);
    }

    private static bool LooksLikeTotal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var n = NormalizeHeader(s);
        return n is "TOTAL" or "SUMA" || n.StartsWith("TOTAL ", StringComparison.Ordinal);
    }

    private static int FindHeaderRow(List<List<string>> matrix)
    {
        var limit = Math.Min(matrix.Count, 15);
        for (var i = 0; i < limit; i++)
        {
            if (MapColumns(matrix[i]).IsValid) return i;
        }

        return -1;
    }

    private static int FindFirstDataRow(List<List<string>> matrix)
    {
        for (var i = 0; i < matrix.Count; i++)
        {
            var tipo = matrix[i].Count > 0 ? matrix[i][0] : string.Empty;
            if (EsTePago(tipo) || EsPagaste(tipo)) return i;
        }

        return -1;
    }

    private readonly struct ColumnMap(int tipo, int origen, int monto, int mensaje, int fecha)
    {
        public int Tipo { get; } = tipo;
        public int Origen { get; } = origen;
        public int Monto { get; } = monto;
        public int Mensaje { get; } = mensaje;
        public int Fecha { get; } = fecha;
        public bool IsValid => Tipo >= 0 && Origen >= 0 && Monto >= 0 && Fecha >= 0;

        public static ColumnMap DefaultPositional() => new(0, 1, 2, 3, 4);
    }

    private static ColumnMap MapColumns(List<string> header)
    {
        int tipo = -1, origen = -1, monto = -1, mensaje = -1, fecha = -1;
        for (var i = 0; i < header.Count; i++)
        {
            var h = NormalizeHeader(header[i]);
            if (h.Length == 0) continue;

            if (tipo < 0 && (h.Contains("TIPO", StringComparison.Ordinal) || h.Contains("TRANSACCION", StringComparison.Ordinal)))
                tipo = i;
            else if (origen < 0 && (
                         h.Contains("ORIGEN", StringComparison.Ordinal) ||
                         h.Contains("DESTINATARIO", StringComparison.Ordinal) ||
                         h.Contains("DESTINO", StringComparison.Ordinal) ||
                         h.Contains("NOMBRE", StringComparison.Ordinal) ||
                         h is "DE" or "PARA"))
                origen = i;
            else if (monto < 0 && (h.Contains("MONTO", StringComparison.Ordinal) || h.Contains("IMPORTE", StringComparison.Ordinal) || h == "S/" || h.Contains("CANTIDAD", StringComparison.Ordinal)))
                monto = i;
            else if (mensaje < 0 && (h.Contains("MENSAJE", StringComparison.Ordinal) || h.Contains("GLOSA", StringComparison.Ordinal) || h.Contains("NOTA", StringComparison.Ordinal) || h.Contains("REFERENCIA", StringComparison.Ordinal) || h.Contains("CONCEPTO", StringComparison.Ordinal)))
                mensaje = i;
            else if (fecha < 0 && (h.Contains("FECHA", StringComparison.Ordinal) || h.Contains("HORA", StringComparison.Ordinal) || h.Contains("OPERACION", StringComparison.Ordinal)))
                fecha = i;
        }

        return new ColumnMap(tipo, origen, monto, mensaje, fecha);
    }

    private static string NormalizeHeader(string value)
    {
        var s = value.Trim().ToUpperInvariant();
        s = s.Replace('Ó', 'O').Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ú', 'U')
            .Replace('Ñ', 'N');
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }

    private static string Get(List<string> row, int index)
    {
        if (index < 0 || index >= row.Count) return string.Empty;
        return row[index] ?? string.Empty;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.TryGetValue(out DateTime dt))
        {
            return dt.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (cell.DataType == XLDataType.Number && cell.TryGetValue(out double num))
        {
            return num.ToString("0.##", CultureInfo.InvariantCulture);
        }

        return cell.GetFormattedString()?.Trim() ?? cell.GetString()?.Trim() ?? string.Empty;
    }

    public static bool TryParseMonto(string? raw, out decimal monto)
    {
        monto = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim()
            .Replace("S/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("PEN", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        s = Regex.Replace(s, @"\s+", "");

        if (decimal.TryParse(s, NumberStyles.Number, Peru, out monto))
        {
            monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
            return true;
        }

        if (s.Contains(',') && s.Contains('.'))
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.'))
            {
                s = s.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
            }
            else
            {
                s = s.Replace(",", "", StringComparison.Ordinal);
            }
        }
        else if (s.Contains(',') && !s.Contains('.'))
        {
            s = s.Replace(',', '.');
        }

        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out monto))
        {
            monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
            return true;
        }

        return false;
    }

    public static bool TryParseFecha(string? raw, out DateTime fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim();
        if (DateTime.TryParseExact(s, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
        {
            return true;
        }

        return DateTime.TryParse(s, Peru, DateTimeStyles.None, out fecha);
    }
}
