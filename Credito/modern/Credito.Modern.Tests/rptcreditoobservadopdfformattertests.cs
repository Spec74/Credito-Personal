using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class RptCreditoObservadoPdfFormatterTests
{
    [Fact]
    public void ToPdf_vacio_devuelve_pdf_valido()
    {
        var bytes = RptCreditoObservadoPdfFormatter.ToPdf(Array.Empty<RptCreditoObservadoRowDto>());
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 200);
    }

    [Fact]
    public void ToPdf_con_fila_contiene_credito_id_en_contenido()
    {
        var rows = new[]
        {
            new RptCreditoObservadoRowDto
            {
                OficinaId = 1,
                Oficina = "Central",
                CreditoId = 424242,
                Cliente = "Cliente Piloto",
                FechaPrimerPago = new DateTime(2024, 1, 15, 10, 0, 0),
                FechaVencimiento = new DateTime(2024, 6, 15, 10, 0, 0),
                MontoCredito = 1500.5m,
                Interes = 12.25m,
                AgenteId = 9,
                Agente = "Gestor A",
                Observacion = "Obs piloto",
                TramiteAdm = 10m,
                CentralRiesgo = 5m,
            },
        };

        var bytes = RptCreditoObservadoPdfFormatter.ToPdf(rows);
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
