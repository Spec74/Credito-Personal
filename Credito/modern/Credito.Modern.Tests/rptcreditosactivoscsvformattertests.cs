using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditosActivosCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditosActivosCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditosActivosRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Nro,Estado,Agente,CreditoId", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditosActivosRowDto>
        {
            new()
            {
                Nro = 1,
                Estado = "ACT",
                Agente = "Gestor 1",
                CreditoId = 300,
                Cliente = "Torres, Ana",
                MontoCredito = 4000m,
                NumeroCuotas = 10,
                Interes = 20m,
                FechaPrimerPago = fecha,
                FechaVencimiento = fecha.AddMonths(10),
                InteresPagado = 500m,
                CentralRiesgo = 0m,
                MontoGastosAdm = 40m,
            },
        };
        var line = DataLine(RptCreditosActivosCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("300", line);
        Assert.Contains("\"Torres, Ana\"", line);
        Assert.Contains(",ACT,", line);
    }
}
