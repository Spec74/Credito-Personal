using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public class InformeFichaPdfDocumentTests
{
    [Fact]
    public void Cliente_ficha_pdf_genera_bytes()
    {
        var informe = new RptClienteInformeDto(
            new RptClienteFichaDto(
                1,
                2,
                "Juan Pérez",
                "12345678",
                "01/01/1990",
                "M",
                "Av. Principal",
                null,
                "999888777",
                "María",
                "87654321",
                "888777666",
                "Propia",
                "Casado",
                "Lima",
                "Comercio",
                null,
                null,
                null),
            []);
        var bytes = RptClienteFichaPdfDocument.Build(informe);
        Assert.NotEmpty(bytes);
        Assert.Equal('%', (char)bytes[0]);
    }

    [Fact]
    public void Simulador_plan_pdf_genera_bytes()
    {
        var dto = new RptSimuladorPlanPagosInformeDto(
            new RptSimuladorPlanPagosCabeceraDto(
                "1000",
                "12",
                "Producto X",
                "01/06/2026",
                "M",
                "Cliente",
                "5%",
                "950",
                "50",
                "DNI",
                "12345678",
                "Av. Principal",
                "Jr. Negocio",
                "Ninguna",
                "Asesor",
                "999888777",
                "S/. 20.00",
                "S/. 1020.00",
                "S/. 100.00",
                "01/06/2026"),
            [
                new SimuladorCreditoCuotaDto
                {
                    Numero = 1,
                    Capital = 1000,
                    FechaPago = new DateTime(2026, 6, 1),
                    Amortizacion = 80,
                    Interes = 20,
                    GastosAdm = 0,
                    Cuota = 100,
                    Saldo = 920,
                },
            ]);
        var bytes = RptSimuladorPlanPagosPdfDocument.Build(dto);
        Assert.NotEmpty(bytes);
    }
}
