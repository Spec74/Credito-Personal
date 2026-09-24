using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public sealed class RptSaldoCarteraCajaDiarioPdfDocumentTests
{
    [Fact]
    public void Build_vacio_devuelve_pdf_valido()
    {
        var bytes = RptSaldoCarteraCajaDiarioPdfDocument.Build(
            Array.Empty<RptSaldoCarteraCajaDiarioRowDto>(),
            new RptSaldoCarteraCajaDiarioPdfDocument.Header("Central", "agosto 2026", "septiembre 2026"));
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 800);
    }

    [Fact]
    public void Build_con_fila_incluye_periodos()
    {
        var rows = new[]
        {
            new RptSaldoCarteraCajaDiarioRowDto
            {
                Oficina = "Oficina Principal",
                Caja = "Caja 1",
                Agente = "Gestor Demo",
                FechaCierreIni = new DateTime(2026, 8, 31),
                SalidasIni = 1000m,
                MontoCobradoIni = 2500.5m,
                PocentajeCobroIni = 80m,
                SaldoCarteraSinMoraIni = 12000m,
                NroClientesCarteraSinMoraIni = 9,
                FechaCierreFin = new DateTime(2026, 9, 12),
                SalidasFin = 800m,
                MontoCobradoFin = 3100m,
                PocentajeCobroFin = 82m,
                SaldoCarteraSinMoraFin = 11000m,
                NroClientesCarteraSinMoraFin = 8,
            },
        };

        var bytes = RptSaldoCarteraCajaDiarioPdfDocument.Build(
            rows,
            new RptSaldoCarteraCajaDiarioPdfDocument.Header("Oficina Principal", "agosto 2026", "septiembre 2026"));
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 1200);
    }

    [Fact]
    public void Tablas_anchas_no_caben_en_A4()
    {
        var wide = CredixLegacyPdfDocument.ResolvePageSize(26, landscape: true);
        var a4 = CredixLegacyPdfDocument.ResolvePageSize(8, landscape: true);
        Assert.True(wide.Width > a4.Width);
        Assert.Equal(6.2f, CredixLegacyPdfDocument.ResolveBodyFont(26));
    }

    [Fact]
    public void Tablas_cortas_en_A4_aunque_se_pida_apaisado()
    {
        Assert.False(CredixLegacyPdfDocument.EffectiveLandscape(8, preferLandscape: true));
        var shortPage = CredixLegacyPdfDocument.ResolvePageSize(8, landscape: false);
        Assert.True(shortPage.Width < shortPage.Height);
        Assert.Equal(CredixLegacyPdfDocument.FontSizeBody, CredixLegacyPdfDocument.ResolveBodyFont(8));
    }

    private static void AssertPdfMagic(byte[] bytes)
    {
        Assert.True(bytes.Length >= 4);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
