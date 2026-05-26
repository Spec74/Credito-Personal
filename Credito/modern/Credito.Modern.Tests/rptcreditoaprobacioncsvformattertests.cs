using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoAprobacionCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoAprobacionCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoAprobacionRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("CreditoId,Oficina,Cliente,FechaAprobacion", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 5, 1, 14, 30, 0, DateTimeKind.Unspecified);
        var rows = new List<RptCreditoAprobacionRowDto>
        {
            new()
            {
                CreditoId = 201,
                Oficina = "Sur",
                Cliente = "Méndez, José",
                FechaAprobacion = fecha,
                MontoCredito = 8000m,
                Interes = 15m,
                NumeroCuotas = 12,
                MontoDesembolso = 7900m,
                Gestor = "Gestor A",
            },
        };
        var line = DataLine(RptCreditoAprobacionCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("201", line);
        Assert.Contains("\"Méndez, José\"", line);
        Assert.Contains("Gestor A", line);
    }
}
