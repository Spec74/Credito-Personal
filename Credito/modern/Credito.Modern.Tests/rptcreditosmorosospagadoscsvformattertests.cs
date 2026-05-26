using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditosMorososPagadosCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditosMorososPagadosCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditosMorososPagadosRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CreditoId,Cliente,MontoCredito", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditosMorososPagadosRowDto>
        {
            new()
            {
                CreditoId = 501,
                Cliente = "Castro, Eva",
                MontoCredito = 3500m,
                Interes = 18m,
                FormaPago = "M",
                NumeroCuotas = 12,
                MontoGastosAdm = 35m,
                CentralRiesgo = 0m,
                FechaPrimerPago = fecha,
                FechaVencimiento = fecha.AddMonths(12),
                FechaPagado = fecha.AddMonths(11),
                Agente = "Gestor 5",
            },
        };
        var line = DataLine(RptCreditosMorososPagadosCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("501", line);
        Assert.Contains("\"Castro, Eva\"", line);
        Assert.Contains("Gestor 5", line);
    }
}
