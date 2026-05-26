using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Producto,Cliente,CreditoId", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditoRowDto>
        {
            new()
            {
                Producto = "Personal",
                Cliente = "Ruiz, María",
                CreditoId = 100,
                FechaDesembolso = fecha,
                FechaVcto = fecha.AddMonths(12),
                FormaPago = "M",
                NumeroCuotas = 12,
                Interes = 18m,
                Estado = "DES",
                MontoProducto = 5000m,
                MontoInicial = 0m,
                MontoCredito = 5000m,
                MontoGastosAdm = 50m,
                MontoDesembolso = 4950m,
            },
        };
        var line = DataLine(RptCreditoCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("100", line);
        Assert.Contains("\"Ruiz, María\"", line);
        Assert.Contains(",DES,", line);
    }
}
