using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoMorosidadCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoMorosidadCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoMorosidadRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CreditoId,Cliente,Direccion", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditoMorosidadRowDto>
        {
            new()
            {
                CreditoId = 77,
                Cliente = "Vega, Rosa",
                MontoCredito = 3000m,
                DiasAtrazo = 15,
                DeudaAtrazo = 500m,
                FechaDesembolso = fecha,
            },
        };
        var line = DataLine(RptCreditoMorosidadCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("77", line);
        Assert.Contains("\"Vega, Rosa\"", line);
        Assert.Contains(",15,", line);
        Assert.EndsWith("500", line);
    }
}
