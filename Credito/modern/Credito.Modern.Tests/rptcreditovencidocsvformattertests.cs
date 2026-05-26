using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoVencidoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoVencidoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoVencidoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Gestor,CreditoId,Cliente", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditoVencidoRowDto>
        {
            new()
            {
                Gestor = "Gestor 2",
                CreditoId = 301,
                Cliente = "Ruiz, Pedro",
                MontoCredito = 4200m,
                FormaPago = "M",
                FechaVencimiento = fecha,
                CreditoVencido = 1500.75m,
                VencidoMenor60 = "S",
                VencidoMayor60 = "N",
                VencidoIrrecuperable = "N",
            },
        };
        var line = DataLine(RptCreditoVencidoCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("301", line);
        Assert.Contains("1500.75", line);
        Assert.Contains("\"Ruiz, Pedro\"", line);
    }
}
