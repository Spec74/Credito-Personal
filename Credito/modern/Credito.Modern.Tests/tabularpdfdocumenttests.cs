using Credito.Modern.Application;
using Credito.Modern.Application;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public sealed class TabularPdfDocumentTests
{
    [Fact]
    public void FromUtf8BomCsv_vacio_genera_pdf_valido()
    {
        var csv = RptCreditoObservadoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptCreditoObservadoRowDto>());
        var pdf = TabularPdfDocument.FromUtf8BomCsv("Créditos observados", csv);
        AssertPdfMagic(pdf);
    }

    [Fact]
    public void FromUtf8BomCsv_ignora_linea_sep_excel()
    {
        var csv = CsvUtf8BomEncoding.GetBytes("CreditoId,Cliente\r\n1,Uno\r\n");
        var pdf = TabularPdfDocument.FromUtf8BomCsv("Test sep", csv);
        AssertPdfMagic(pdf);
    }
    [Fact]
    public void FromUtf8BomCsv_con_fila_genera_pdf_valido()
    {
        var rows = new[]
        {
            new RptCreditoCondonadoRowDto
            {
                OficinaId = 1,
                CreditoId = 99,
                Cliente = "Test",
                FechaPrimerPago = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow,
                MontoCredito = 1,
                Interes = 0,
                MontoCondonado = 1,
                AgenteId = 1,
            },
        };
        var csv = RptCreditoCondonadoCsvFormatter.ToUtf8BomCsv(rows);
        var pdf = TabularPdfDocument.FromUtf8BomCsv("Créditos condonados", csv);
        AssertPdfMagic(pdf);
        Assert.True(pdf.Length > 300);
    }

    private static void AssertPdfMagic(byte[] bytes)
    {
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
