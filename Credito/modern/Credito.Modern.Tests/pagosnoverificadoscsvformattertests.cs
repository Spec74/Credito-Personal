using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class PagosNoVerificadosCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = PagosNoVerificadosCsvFormatter.ToUtf8BomCsv(Array.Empty<PagosNoVerificadosRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("MovimientoCajaId,Cliente,Movimiento,ImportePago,TipoPago,FechaTransferencia,Registro", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila_y_escapa_campos()
    {
        var rows = new List<PagosNoVerificadosRowDto>
        {
            new()
            {
                MovimientoCajaId = 7,
                Cliente = "Pérez, Ana",
                Movimiento = "Pago",
                ImportePago = 100.5m,
                TipoPago = "Yape",
                FechaTransferencia = "2026-01-02",
                Registro = "R1",
            },
        };
        var line = DataLine(PagosNoVerificadosCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Equal(string.Join(',', "7", "\"Pérez, Ana\"", "Pago", "100.5", "Yape", "2026-01-02", "R1"), line);
    }
}
