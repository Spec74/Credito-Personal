using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCobroDiarioDetalleCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCobroDiarioDetalleCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCobroDiarioDetalleRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Nro,Cliente,FormaPago,MontoCredito", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCobroDiarioDetalleRowDto>
        {
            new()
            {
                Nro = 1,
                Cliente = "García, Pedro",
                FormaPago = "Q",
                MontoCredito = 500m,
                Interes = 25m,
                MontoTotal = 525m,
                FechaPrimerPago = fecha,
                FechaVencimiento = fecha.AddMonths(3),
                Saldo = 100m,
                TotalPago = 425m,
                DiasAtrazoMora = 0,
                Pagos = "3/3",
            },
        };
        var line = DataLine(RptCobroDiarioDetalleCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("\"García, Pedro\"", line);
        Assert.Contains(",Q,", line);
        Assert.EndsWith("3/3", line);
    }
}
