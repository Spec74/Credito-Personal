using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditosCierresCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditosCierresCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditosCierresRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CreditoId,Estado,Agente,Codigo", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditosCierresRowDto>
        {
            new()
            {
                CreditoId = 88,
                Estado = "CIE",
                Agente = "Gestor 3",
                Cliente = "Ramos, Luis",
                MontoCredito = 2500m,
                NumeroCuotas = 8,
                Interes = 16m,
                MontoGastosAdm = 25m,
                CentralRiesgo = 0m,
                FechaPrimerPago = fecha,
                FechaVencimiento = fecha.AddMonths(8),
                SumCuota = 3200m,
                SumNroCuota = 8,
            },
        };
        var line = DataLine(RptCreditosCierresCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("88", line);
        Assert.Contains("\"Ramos, Luis\"", line);
        Assert.EndsWith("8", line);
    }
}
