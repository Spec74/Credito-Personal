using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptMovimientoBovedaCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptMovimientoBovedaCsvFormatter.ToUtf8BomCsv(Array.Empty<RptMovimientoBovedaRowDto>());
        AssertUtf8Bom(bytes);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("MovimientoBovedaId,FechaReg,CodOperacion", text, StringComparison.Ordinal);
        Assert.StartsWith("MovimientoBovedaId", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 3, 8, 9, 45, 0, DateTimeKind.Unspecified);
        var rows = new List<RptMovimientoBovedaRowDto>
        {
            new()
            {
                MovimientoBovedaId = 12,
                FechaReg = fecha,
                CodOperacion = "ING",
                Glosa = "Depósito, caja 2",
                Entrada = 5000m,
                Salida = null,
                TipoPago = "EFECTIVO",
                Agente = "Gestor 1",
            },
        };
        var line = DataLine(RptMovimientoBovedaCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("12", line);
        Assert.Contains("5000", line);
        Assert.Contains("\"Depósito, caja 2\"", line);
    }
}
