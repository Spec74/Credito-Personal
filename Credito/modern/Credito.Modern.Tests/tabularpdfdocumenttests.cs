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

    [Fact]
    public void CobroDiarioPdfDocument_con_fila_genera_pdf_profesional_valido()
    {
        var rows = new[]
        {
            new RptCobroDiarioRowDto
            {
                Nro = 1,
                Cliente = "CLIENTE PRUEBA",
                Celular = "999999999",
                MontoCredito = 1000m,
                Interes = 8m,
                CuotaPlan = 108m,
                Saldo = 432m,
                DiasAtrazo = 10,
                NroCuotasPen = 4,
                CuotaTotal = 108m,
                Direccion = "JR PRUEBA 123",
                FechaPrimerPago = new DateTime(2026, 1, 1),
                FechaVencimiento = new DateTime(2026, 6, 1),
                Mora = 12.5m,
                MontoTotal = 120.5m,
                Negocio = "BODEGA",
                FormaPago = "D",
                TopeCredito = 1000m,
                ClasificacionRiesgoSBS = "NOR",
            },
        };
        var context = new CredixLegacyReportContext
        {
            Agente = "GESTOR PRUEBA",
            Caja = "CAJA PRUEBA",
        };

        var pdf = RptCobroDiarioPdfDocument.Build(rows, context, soloMora: false);

        AssertPdfMagic(pdf);
        Assert.True(pdf.Length > 1000);
    }

    private static void AssertPdfMagic(byte[] bytes)
    {
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
