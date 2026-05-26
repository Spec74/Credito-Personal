using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Tests;

/// <summary>Utilidades para asserts de CSV con BOM y línea <c>sep=,</c> (Excel es-PE).</summary>
public static class CsvTestHelpers
{
    public static void AssertUtf8Bom(byte[] bytes)
    {
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    public static string Decode(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    public static IReadOnlyList<string> LogicalLines(byte[] bytes)
    {
        return Decode(bytes)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim('\r', '\uFEFF'))
            .Where(l =>
                !string.Equals(l, CsvUtf8BomEncoding.ExcelSeparatorHintLine, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static string HeaderLine(byte[] bytes) => LogicalLines(bytes)[0];

    public static string DataLine(byte[] bytes, int dataRowIndex = 0) =>
        LogicalLines(bytes)[1 + dataRowIndex];
}
