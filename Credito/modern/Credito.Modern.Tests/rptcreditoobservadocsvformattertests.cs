using System.Globalization;
using System.Text;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptCreditoObservadoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptCreditoObservadoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoObservadoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("OficinaId,Oficina,CreditoId,Cliente,FechaPrimerPago,FechaVencimiento,MontoCredito,Interes,AgenteId,Agente,Observacion,TramiteAdm,CentralRiesgo", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new[]
        {
            new RptCreditoObservadoRowDto
            {
                OficinaId = 1,
                Oficina = "O",
                CreditoId = 99,
                Cliente = "C",
                FechaPrimerPago = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Unspecified),
                FechaVencimiento = new DateTime(2027, 1, 10, 0, 0, 0, DateTimeKind.Unspecified),
                MontoCredito = 1000m,
                Interes = 10m,
                AgenteId = 5,
                Agente = "A",
                Observacion = "Obs",
                TramiteAdm = 2m,
                CentralRiesgo = 3m,
            },
        };
        var bytes = RptCreditoObservadoCsvFormatter.ToUtf8BomCsv(rows);
        var inv = CultureInfo.InvariantCulture;
        var line = DataLine(bytes);
        Assert.Equal(
            string.Join(
                ',',
                "1",
                "O",
                "99",
                "C",
                ReportCsvFormats.FormatDateOrDateTime(new DateTime(2026, 1, 10), inv),
                ReportCsvFormats.FormatDateOrDateTime(new DateTime(2027, 1, 10), inv),
                "1000",
                "10",
                "5",
                "A",
                "Obs",
                "2",
                "3"),
            line);
    }
}
