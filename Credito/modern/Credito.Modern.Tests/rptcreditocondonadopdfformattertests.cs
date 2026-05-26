using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class RptCreditoCondonadoPdfFormatterTests
{
    [Fact]
    public void ToPdf_vacio_devuelve_pdf_valido()
    {
        var bytes = RptCreditoCondonadoPdfFormatter.ToPdf(Array.Empty<RptCreditoCondonadoRowDto>());
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 200);
    }

    [Fact]
    public void ToPdf_con_fila_incluye_monto_condonado()
    {
        var rows = new[]
        {
            new RptCreditoCondonadoRowDto
            {
                OficinaId = 1,
                Oficina = "Central",
                CreditoId = 1001,
                Cliente = "Cliente Condonado",
                FechaPrimerPago = new DateTime(2024, 2, 1, 8, 0, 0),
                FechaVencimiento = new DateTime(2024, 8, 1, 8, 0, 0),
                MontoCredito = 2000m,
                Interes = 50m,
                MontoCondonado = 1999.99m,
                AgenteId = 3,
                Agente = "Gestor B",
                Observacion = "Condonación piloto",
            },
        };

        var bytes = RptCreditoCondonadoPdfFormatter.ToPdf(rows);
        AssertPdfMagic(bytes);
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
