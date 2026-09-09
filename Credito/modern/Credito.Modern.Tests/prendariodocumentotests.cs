using Credito.Modern.Application.Prendario;

namespace Credito.Modern.Tests;

public sealed class PrendarioDocumentoTests
{
    [Theory]
    [InlineData(0, "CERO CON 00/100 SOLES")]
    [InlineData(1, "UN CON 00/100 SOLES")]
    [InlineData(21, "VEINTIUN CON 00/100 SOLES")]
    [InlineData(100, "CIEN CON 00/100 SOLES")]
    [InlineData(300, "TRESCIENTOS CON 00/100 SOLES")]
    [InlineData(1500.5, "MIL QUINIENTOS CON 50/100 SOLES")]
    public void NumeroALetras_en_soles(decimal monto, string esperado)
    {
        Assert.Equal(esperado, NumeroALetras.EnSoles(monto));
    }

    [Fact]
    public void Contrato_genera_pdf_valido()
    {
        var dto = DocumentoDePrueba();
        var pdf = RptContratoPrendarioPdfDocument.Build(dto);
        AssertPdf(pdf);
    }

    [Fact]
    public void Acta_genera_pdf_valido()
    {
        var contrato = DocumentoDePrueba();
        var acta = new PrendarioActaDto(
            contrato.CreditoId,
            contrato.NumeroContrato,
            contrato.FechaEmision,
            contrato.ApellidosNombres,
            contrato.DniCliente,
            contrato.Domicilio,
            contrato.Distrito,
            "HUAMANGA",
            "AYACUCHO",
            contrato.Bienes);
        var pdf = RptActaEntregaPrendarioPdfDocument.Build(acta);
        AssertPdf(pdf);
    }

    private static PrendarioContratoDto DocumentoDePrueba() =>
        new(
            80392,
            "80392",
            new DateTime(2026, 6, 18),
            new DateTime(2026, 7, 18),
            new DateTime(2026, 8, 17),
            new DateTime(2026, 6, 18),
            "1 MES",
            "CLIENTE PRUEBA",
            "12345678",
            null,
            null,
            null,
            "999999999",
            "JR PRUEBA 123",
            "AYACUCHO",
            null,
            300m,
            200m,
            8m,
            16m,
            0.53m,
            10m,
            "ANALISTA PRUEBA",
            [
                new PrendarioBienDocumentoDto("JOYA", null, null, "N/T", null, 300m, null),
            ]);

    private static void AssertPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 300);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
