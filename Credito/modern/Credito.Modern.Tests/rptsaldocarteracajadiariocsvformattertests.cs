using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptSaldoCarteraCajaDiarioCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptSaldoCarteraCajaDiarioCsvFormatter.ToUtf8BomCsv(Array.Empty<RptSaldoCarteraCajaDiarioRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("AgenteId,Oficina,Caja", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 1, 31, 18, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptSaldoCarteraCajaDiarioRowDto>
        {
            new()
            {
                AgenteId = 7,
                Oficina = "Sur",
                Caja = "Caja A",
                Agente = "Torres, Luis",
                FechaCierreIni = fecha,
                SalidasIni = 500m,
                MontoCobradoIni = 12000.25m,
                PocentajeCobroIni = 85.5m,
                SaldoCarteraSinMoraIni = 80000m,
                NroClientesCarteraSinMoraIni = 40,
                SaldoMoraCarteraIni = 5000m,
                NroClientesSaldoMoraCarteraIni = 3,
                NroClientesNuevosIni = 2,
                SaldoVencidoIni = 1500m,
                SaldoMorosidadIni = 3200m,
                FechaCierreFin = fecha.AddMonths(2),
                SalidasFin = 600m,
                MontoCobradoFin = 13500m,
                PocentajeCobroFin = 88m,
                SaldoCarteraSinMoraFin = 75000m,
                NroClientesCarteraSinMoraFin = 38,
                SaldoMoraCarteraFin = 6200m,
                NroClientesSaldoMoraCarteraFin = 4,
                NroClientesNuevosFin = 5,
                SaldoVencidoFin = 1800m,
                SaldoMorosidadFin = 4100m,
            },
        };
        var line = DataLine(RptSaldoCarteraCajaDiarioCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("7", line);
        Assert.Contains("12000.25", line);
        Assert.Contains("\"Torres, Luis\"", line);
    }
}
