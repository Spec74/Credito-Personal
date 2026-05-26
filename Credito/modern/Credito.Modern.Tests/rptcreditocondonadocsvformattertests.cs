using System.Globalization;
using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoCondonadoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoCondonadoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoCondonadoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("OficinaId,Oficina,CreditoId,Cliente,FechaPrimerPago,FechaVencimiento,MontoCredito,Interes,MontoCondonado,AgenteId,Agente,Observacion", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new[]
        {
            new RptCreditoCondonadoRowDto
            {
                OficinaId = 1,
                Oficina = "O",
                CreditoId = 50,
                Cliente = "C",
                FechaPrimerPago = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Unspecified),
                FechaVencimiento = new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Unspecified),
                MontoCredito = 200m,
                Interes = 20m,
                MontoCondonado = 5m,
                AgenteId = 9,
                Agente = "A",
                Observacion = "X",
            },
        };
        var bytes = RptCreditoCondonadoCsvFormatter.ToUtf8BomCsv(rows);
        var inv = CultureInfo.InvariantCulture;
        var fmt = "yyyy-MM-dd HH:mm:ss";
        var line = DataLine(bytes);
        Assert.Equal(
            string.Join(
                ',',
                "1",
                "O",
                "50",
                "C",
                new DateTime(2026, 2, 1).ToString(fmt, inv),
                new DateTime(2027, 2, 1).ToString(fmt, inv),
                "200",
                "20",
                "5",
                "9",
                "A",
                "X"),
            line);
    }
}
