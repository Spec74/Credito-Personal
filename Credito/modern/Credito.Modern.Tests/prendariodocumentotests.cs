using Credito.Modern.Application.Prendario;
using Credito.Modern.Application.Reportes;

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
    public void Fecha_de_clausula_usa_mes_en_espanol()
    {
        Assert.Equal(
            "Ayacucho, 18 de junio de 2026",
            PrendarioPdfTexto.FechaCiudad(new DateTime(2026, 6, 18)));
    }

    [Fact]
    public void Anexo_B_usa_mes_capitalizado_y_plazo_en_partes()
    {
        Assert.Equal(
            "jueves, 30 de Enero de 2025",
            PrendarioPdfTexto.FechaLargaConDia(new DateTime(2025, 1, 30)));
        Assert.Equal("18%", PrendarioPdfTexto.PorcentajeEntero(18m));
        Assert.Equal(("1", "MES"), PrendarioPdfTexto.PartesPlazo("1 MES"));
        Assert.Equal("CON 80/100 SOL", NumeroALetras.EnSoles(0.80m));
    }

    [Fact]
    public void Contrato_anexa_clausulas_oficiales_y_estampa_fecha()
    {
        var soloAnexo = CredixReportAssets.LoadClausulasPrendario();
        Assert.NotNull(soloAnexo);
        var pdf = RptContratoPrendarioPdfDocument.Build(DocumentoDePrueba());
        AssertPdf(pdf);
        Assert.True(pdf.Length > soloAnexo!.Length);

        AssertPdf(PrendarioPdfMerge.BuildSello(
            new DateTime(2026, 6, 18),
            "CLIENTE PRUEBA",
            "12345678"));

        var paginasClausulas = PrendarioPdfMerge.ContarPaginas(soloAnexo);
        Assert.True(paginasClausulas >= 1);
        Assert.Equal(2 + paginasClausulas, PrendarioPdfMerge.ContarPaginas(pdf));
        AssertPdf(PrendarioPdfMerge.BuildNumeracion(2 + paginasClausulas));
    }

    [Fact]
    public void Acta_incluye_fecha_y_secciones_del_rdlc()
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
        Assert.Equal(1, PrendarioPdfMerge.ContarPaginas(pdf));
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
                new PrendarioBienDocumentoDto("JOYA", null, null, "N/T", null, 300m, null, null),
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
