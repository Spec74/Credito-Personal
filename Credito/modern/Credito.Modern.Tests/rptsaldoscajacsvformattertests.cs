using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptSaldosCajaCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptSaldosCajaCsvFormatter.ToUtf8BomCsv(Array.Empty<RptSaldosCajaRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("MovimientoCajaId,Operacion,FechaReg", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 2, 5, 10, 15, 0, DateTimeKind.Unspecified);
        var rows = new List<RptSaldosCajaRowDto>
        {
            new()
            {
                MovimientoCajaId = 901,
                Operacion = "COBRO",
                FechaReg = fecha,
                Codigo = "C-100",
                Cliente = "Méndez, Ana",
                ImportePago = 350.75m,
                IndEntrada = true,
                Glosa = "Pago cuota",
                TipoPago = "EFECTIVO",
            },
        };
        var line = DataLine(RptSaldosCajaCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("901", line);
        Assert.Contains("350.75", line);
        Assert.Contains("True", line);
        Assert.Contains("\"Méndez, Ana\"", line);
    }
}
