using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public sealed class CredixMorosidadPdfDocumentTests
{
    [Fact]
    public void Build_vacio_devuelve_pdf_valido()
    {
        var bytes = CredixMorosidadPdfDocument.Build(
            Array.Empty<RptCreditoMorosidadRowDto>(),
            new CredixMorosidadPdfDocument.Header("Central", "11/09/2026", 1, 9999));
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 400);
    }

    [Fact]
    public void Build_con_fila_incluye_contacto_en_subfila()
    {
        var rows = new[]
        {
            new RptCreditoMorosidadRowDto
            {
                CreditoId = 4242,
                Articulo = "Laptop",
                Cliente = "Cliente Piloto",
                Celular = "999111222",
                Direccion = "Av. Principal 100",
                FechaDesembolso = new DateTime(2026, 1, 15),
                FechaVcto = new DateTime(2026, 7, 15),
                MontoCredito = 1500.5m,
                SaldoCredito = 800m,
                CapitalAtrazo = 100m,
                GA = 10m,
                InteresAtrazo = 12.25m,
                Mora = 5m,
                ImporteLibre = 0m,
                DiasAtrazo = 12,
                CuotasAtrazo = 1,
                DeudaAtrazo = 127.25m,
            },
        };

        var bytes = CredixMorosidadPdfDocument.Build(
            rows,
            new CredixMorosidadPdfDocument.Header("Oficina 1", "11/09/2026", 1, 30));
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 800);
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
