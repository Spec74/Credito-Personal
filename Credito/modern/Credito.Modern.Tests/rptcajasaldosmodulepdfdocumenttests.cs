using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public sealed class RptCajaSaldosModulePdfDocumentTests
{
    [Fact]
    public void Cajas_asignadas_vacio_es_pdf_valido()
    {
        var bytes = RptCajasAsignadasPdfDocument.Build(
            Array.Empty<RptCajasAsignadasRowDto>(),
            new CredixLegacyReportContext { Oficina = "Oficina Principal" },
            "EFECTIVO = 100.00  YAPE = 50.00");
        AssertPdfMagic(bytes);
    }

    [Fact]
    public void Cajas_asignadas_con_fila_es_pdf_valido()
    {
        var bytes = RptCajasAsignadasPdfDocument.Build(
            [
                new RptCajasAsignadasRowDto
                {
                    CajaDiarioId = 29087,
                    Caja = "SAN JOSE",
                    Modo = "ABIERTO",
                    Cajero = "JARA CACERES, KEYLA",
                    FechaIniOperacion = new DateTime(2026, 9, 1, 9, 27, 0),
                    SaldoInicial = 100m,
                    Entradas = 1065m,
                    Salidas = 500m,
                    SaldoFinal = 665m,
                    Resumen = "EFECTIVO = 665.00",
                },
            ],
            new CredixLegacyReportContext { Oficina = "Oficina Principal" });
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 1200);
    }

    [Fact]
    public void Saldos_caja_con_cabecera_e_ingresos_es_pdf_valido()
    {
        var cab = new RptSaldoCajaCabDto
        {
            Oficina = "Oficina Principal",
            Cajero = "demo - Demo Persona - SAN JOSE",
            Estado = "ABIERTO",
            Fecha = new DateTime(2026, 9, 1),
            SaldoInicial = 100m,
            SaldoFinal = 665m,
            PorcentajeCobro = 80m,
        };
        var rows = new[]
        {
            new RptSaldosCajaRowDto
            {
                MovimientoCajaId = 1,
                Operacion = "CUO",
                FechaReg = new DateTime(2026, 9, 1, 10, 0, 0),
                Codigo = "C-1",
                Cliente = "Cliente demo",
                ImportePago = 200m,
                IndEntrada = true,
                Glosa = "Cobro cuota",
                TipoPago = "EFECTIVO",
            },
            new RptSaldosCajaRowDto
            {
                MovimientoCajaId = 2,
                Operacion = "TRS",
                FechaReg = new DateTime(2026, 9, 1, 11, 0, 0),
                Cliente = "Oficina",
                ImportePago = 50m,
                IndEntrada = false,
                Glosa = "Transferencia",
                TipoPago = "EFECTIVO",
            },
        };

        var bytes = RptSaldosCajaPdfDocument.Build(rows, cab, "EFECTIVO = 200.00", cajaChica: false);
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 1400);
    }

    [Fact]
    public void Movimiento_boveda_con_cabecera_es_pdf_valido()
    {
        var cab = new BovedaAbiertaDto(
            42,
            1,
            1000m,
            200m,
            50m,
            1150m,
            new DateTime(2026, 9, 1),
            null,
            false,
            false);
        var bytes = RptMovimientoBovedaPdfDocument.Build(
            [
                new RptMovimientoBovedaRowDto
                {
                    MovimientoBovedaId = 9,
                    FechaReg = new DateTime(2026, 9, 1, 12, 0, 0),
                    CodOperacion = "TRE",
                    Glosa = "TRANS DE CAJA",
                    Entrada = 200m,
                    Salida = 0m,
                    TipoPago = "EFECTIVO",
                    Agente = "Demo",
                },
            ],
            new CredixLegacyReportContext { Oficina = "Oficina Principal", Referencia = "Bóveda N° 42" },
            cab,
            "RESUMEN BOVEDA: EFECTIVO = 1000.00  YAPE = 150.00  PLIN = 50.00");
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 1200);
    }

    [Fact]
    public void Resumen_cuenta_formatea_pares_cuenta_importe()
    {
        var linea = ResumenCuentaCajaParser.FormatLine(
            "EFECTIVO = 1065.00  YAPE = 500.00",
            System.Globalization.CultureInfo.GetCultureInfo("es-PE"));
        Assert.Contains("EFECTIVO", linea, StringComparison.Ordinal);
        Assert.Contains("YAPE", linea, StringComparison.Ordinal);
        Assert.DoesNotContain("=", linea, StringComparison.Ordinal);
    }

    [Fact]
    public void Resumen_boveda_compone_efectivo_y_medios_digitales()
    {
        var c = ResumenCuentaCajaParser.Compose(
            "RESUMEN BOVEDA: EFECTIVO = 1000.50  YAPE = 200.00  PLIN = 50.25");
        Assert.Equal(1000.50m, c.Efectivo);
        Assert.Equal(250.25m, c.MediosDigitales);
        Assert.Equal(3, c.Items.Count);
    }

    private static void AssertPdfMagic(byte[] bytes)
    {
        Assert.True(bytes.Length > 800);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
