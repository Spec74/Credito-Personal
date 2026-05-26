using System.Globalization;
using System.Text;
using Credito.Modern.Application.Almacenes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public sealed class GenerarKardexCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = GenerarKardexCsvFormatter.ToUtf8BomCsv(Array.Empty<GenerarKardexRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("MovimientoDetId,Fecha,Concepto,CantEnt,PUEnt,TotalEnt,CantSal,PUSal,TotalSal,CantSaldo,PUSaldo,TotalSaldo", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new List<GenerarKardexRowDto>
        {
            new()
            {
                MovimientoDetId = 10,
                Fecha = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Unspecified),
                Concepto = "ING",
                CantEnt = 2,
                PUEnt = 15.5m,
                TotalEnt = 31m,
                CantSaldo = 2,
                PUSaldo = 15.5m,
                TotalSaldo = 31m,
            },
        };
        var bytes = GenerarKardexCsvFormatter.ToUtf8BomCsv(rows);
        var inv = CultureInfo.InvariantCulture;
        var line = DataLine(bytes);
        Assert.Equal(
            string.Join(
                ',',
                "10",
                new DateTime(2026, 3, 1, 8, 0, 0).ToString("yyyy-MM-dd HH:mm:ss", inv),
                "ING",
                "2",
                "15.5",
                "31",
                string.Empty,
                string.Empty,
                string.Empty,
                "2",
                "15.5",
                "31"),
            line);
    }
}
