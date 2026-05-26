using System.Globalization;
using System.Text;
using Credito.Modern.Application.Ventas;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptRentabilidadVentaCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptRentabilidadVentaCsvFormatter.ToUtf8BomCsv(Array.Empty<RptRentabilidadVentaRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Nro,Codigo,Articulo,MovimientoId,FechaEnt,PrecioEnt,OrdenVentaId,FechaSal,PrecioSal,Modalidad,Rentabilidad,Cliente", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila_invariante()
    {
        var rows = new[]
        {
            new RptRentabilidadVentaRowDto
            {
                Nro = 1,
                Codigo = "X",
                Articulo = "Y",
                MovimientoId = 10,
                FechaEnt = new DateTime(2026, 3, 15, 14, 30, 0, DateTimeKind.Unspecified),
                PrecioEnt = 1.25m,
                OrdenVentaId = 99,
                FechaSal = null,
                PrecioSal = null,
                Modalidad = "C",
                Rentabilidad = 0.5m,
                Cliente = "Z",
            },
        };
        var bytes = RptRentabilidadVentaCsvFormatter.ToUtf8BomCsv(rows);
        var inv = CultureInfo.InvariantCulture;
        var line = DataLine(bytes);
        Assert.Equal(
            string.Join(
                ',',
                "1",
                "X",
                "Y",
                "10",
                new DateTime(2026, 3, 15, 14, 30, 0).ToString("yyyy-MM-dd HH:mm:ss", inv),
                "1.25",
                "99",
                string.Empty,
                string.Empty,
                "C",
                "0.5",
                "Z"),
            line);
    }
}
