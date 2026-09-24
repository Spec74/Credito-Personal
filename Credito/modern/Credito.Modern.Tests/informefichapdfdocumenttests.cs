using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public class InformeFichaPdfDocumentTests
{
    [Fact]
    public void Observado_ficha_pdf_genera_bytes()
    {
        var rows = new List<RptCreditoObservadoRowDto>
        {
            new()
            {
                CreditoId = 10,
                Cliente = "Ana López",
                Oficina = "Ayacucho",
                Agente = "Gestor 1",
                MontoCredito = 1500m,
                Interes = 120m,
                TramiteAdm = 30m,
                CentralRiesgo = 10m,
                FechaPrimerPago = new DateTime(2026, 1, 15),
                FechaVencimiento = new DateTime(2026, 7, 15),
                Observacion = "Pendiente docs",
            },
        };
        var bytes = RptCreditoObservadoFichaPdfDocument.Build(
            rows,
            new CredixLegacyReportContext { Oficina = "Ayacucho", Agente = "Gestor 1" });
        Assert.NotEmpty(bytes);
        Assert.Equal('%', (char)bytes[0]);
    }

    [Fact]
    public void Inactivos_pdf_usa_layout_credix_estandar()
    {
        var rows = new List<RptClientesInactivosRowDto>
        {
            new()
            {
                PersonaId = 1,
                Agente = "JCURO",
                Cliente = "ABREGU MONTERO, ALEJANDRA",
                Dni = "70740322",
                Direccion = "MZ A LOTE 10",
                DireccionRef = "FRENTE A LA FERETERIA",
                Celular = "921215615",
                Calificacion = "A",
                ClasificacionRiesgoSBS = "NORMAL",
                Depurado = "NO",
                DireccionNegocio = "MZ A LOTE 10",
                DireccionNegocioRef = "FRENTE A LA FERETERIA",
                MontoCredito = 300m,
                TopeCredito = 400m,
                TotalCreditos = 7,
                FechaCancelacion = new DateTime(2024, 10, 4),
            },
        };
        var bytes = RptClientesInactivosFichaPdfDocument.Build(
            rows,
            new CredixLegacyReportContext
            {
                Oficina = "OFICINA PRINCIPAL",
                Agente = "CURO FERNANDEZ, JOEL",
            });
        Assert.NotEmpty(bytes);
        Assert.Equal('%', (char)bytes[0]);
        Assert.True(bytes.Length > 800);
    }

    [Fact]
    public void Vencido_ficha_pdf_genera_bytes()
    {
        var rows = new List<RptCreditoVencidoRowDto>
        {
            new()
            {
                CreditoId = 22,
                Gestor = "Gestor 1",
                Cliente = "Luis Soto",
                MontoCredito = 2000m,
                CreditoVencido = 450m,
                FormaPago = "Mensual",
                FechaVencimiento = new DateTime(2026, 3, 1),
                VencidoMenor60 = "S",
                VencidoMayor60 = "N",
                VencidoIrrecuperable = "N",
            },
        };
        var bytes = RptCreditoVencidoFichaPdfDocument.Build(
            rows,
            new CredixLegacyReportContext { Oficina = "Ayacucho", Fecha = "23/09/2026" });
        Assert.NotEmpty(bytes);
        Assert.Equal('%', (char)bytes[0]);
    }

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
                "PEREZ QUISPE, JUAN CARLOS",
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
