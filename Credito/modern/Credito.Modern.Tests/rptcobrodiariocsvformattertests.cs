using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCobroDiarioCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCobroDiarioCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCobroDiarioRowDto>());
        AssertUtf8Bom(bytes);
        var header = HeaderLine(bytes);
        Assert.StartsWith("Nro,Cliente,Celular", header, StringComparison.Ordinal);
        Assert.DoesNotContain("Orden", header);
        Assert.DoesNotContain("CreditoId", header);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila_minima()
    {
        var fecha = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCobroDiarioRowDto>
        {
            new()
            {
                CreditoId = 5,
                MontoCredito = 1000m,
                Interes = 50m,
                FechaPrimerPago = fecha,
                FechaVencimiento = fecha.AddMonths(6),
                FormaPago = "M",
                Cliente = "López, Luis",
            },
        };
        var line = DataLine(RptCobroDiarioCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("5", line);
        Assert.Contains("\"López, Luis\"", line);
        Assert.Contains(",M", line);
    }
}
