using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCajaDiarioCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCajaDiarioCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCajaDiarioRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CajaDiarioId,Oficina,Caja", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fechaIni = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCajaDiarioRowDto>
        {
            new()
            {
                CajaDiarioId = 88,
                Oficina = "Lima Norte",
                Caja = "Caja 1",
                Agente = "Pérez, Luis",
                SaldoInicial = 1000m,
                Entradas = 2500.50m,
                Salidas = 800m,
                SaldoFinal = 2700.50m,
                FechaIniOperacion = fechaIni,
                FechaFinOperacion = fechaIni.AddHours(10),
            },
        };
        var line = DataLine(RptCajaDiarioCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("88", line);
        Assert.Contains("2500.50", line);
        Assert.Contains("\"Pérez, Luis\"", line);
    }
}
