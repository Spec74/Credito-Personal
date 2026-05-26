using System.Globalization;
using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptMovimientoCreditoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptMovimientoCreditoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptMovimientoCreditoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("MovimientoCajaId,Fecha,Operacion,Glosa,ImportePago,Saldo", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new List<RptMovimientoCreditoRowDto>
        {
            new()
            {
                MovimientoCajaId = 100,
                Fecha = new DateTime(2026, 6, 15, 12, 30, 0, DateTimeKind.Unspecified),
                Operacion = "PAGO",
                Glosa = "Cuota",
                ImportePago = 50.25m,
                Saldo = 900m,
            },
        };
        var bytes = RptMovimientoCreditoCsvFormatter.ToUtf8BomCsv(rows);
        var inv = CultureInfo.InvariantCulture;
        var line = DataLine(bytes);
        Assert.Equal(
            string.Join(
                ',',
                "100",
                new DateTime(2026, 6, 15, 12, 30, 0).ToString("yyyy-MM-dd HH:mm:ss", inv),
                "PAGO",
                "Cuota",
                "50.25",
                "900"),
            line);
    }
}
