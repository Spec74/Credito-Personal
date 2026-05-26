using System.Globalization;
using System.Text;
using Credito.Modern.Application.Almacenes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptStockAnuladosCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptStockAnuladosCsvFormatter.ToUtf8BomCsv(Array.Empty<RptStockAnuladoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("MovimientoId,Movimiento,Observacion,Fecha,Cantidad,Detalle", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new[]
        {
            new RptStockAnuladoRowDto
            {
                MovimientoId = 7,
                Movimiento = "M",
                Observacion = "O",
                Fecha = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Unspecified),
                Cantidad = 3,
                Detalle = "D",
            },
        };
        var bytes = RptStockAnuladosCsvFormatter.ToUtf8BomCsv(rows);
        var inv = CultureInfo.InvariantCulture;
        var line = DataLine(bytes);
        Assert.Equal(
            string.Join(
                ',',
                "7",
                "M",
                "O",
                new DateTime(2026, 4, 1, 9, 0, 0).ToString("yyyy-MM-dd HH:mm:ss", inv),
                "3",
                "D"),
            line);
    }
}
