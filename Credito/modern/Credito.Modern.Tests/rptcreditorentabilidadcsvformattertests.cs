using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoRentabilidadCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoRentabilidadCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoRentabilidadRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CreditoId,Oficina,Codigo,Cliente", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditoRentabilidadRowDto>
        {
            new()
            {
                CreditoId = 55,
                Oficina = "Lima",
                Cliente = "Díaz, Carmen",
                FechaDesembolso = fecha,
                NumeroCuotas = 6,
                FormaPago = "M",
                Estado = "DES",
                MontoCredito = 2000m,
                Interes = 12m,
                SumPago = 1800m,
            },
        };
        var line = DataLine(RptCreditoRentabilidadCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("55", line);
        Assert.Contains("\"Díaz, Carmen\"", line);
        Assert.EndsWith("1800", line);
    }
}
