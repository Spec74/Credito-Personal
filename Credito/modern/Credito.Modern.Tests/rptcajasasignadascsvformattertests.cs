using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCajasAsignadasCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCajasAsignadasCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCajasAsignadasRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CajaDiarioId,Caja,Modo,Cajero,FechaIniOperacion,FechaFinOperacion,SaldoInicial,Salidas,Entradas,SaldoFinal,Resumen", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fechaIni = new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Unspecified);
        var fechaFin = new DateTime(2026, 1, 15, 18, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCajasAsignadasRowDto>
        {
            new()
            {
                CajaDiarioId = 10,
                Caja = "Caja 1",
                Modo = "NOR",
                Cajero = "Ana",
                FechaIniOperacion = fechaIni,
                FechaFinOperacion = fechaFin,
                SaldoInicial = 100m,
                Salidas = 20m,
                Entradas = 50m,
                SaldoFinal = 130m,
                Resumen = "OK",
            },
        };
        var line = DataLine(RptCajasAsignadasCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Equal(
            string.Join(
                ',',
                "10",
                "Caja 1",
                "NOR",
                "Ana",
                "2026-01-15 08:30:00",
                "2026-01-15 18:00:00",
                "100",
                "20",
                "50",
                "130",
                "OK"),
            line);
    }
}
