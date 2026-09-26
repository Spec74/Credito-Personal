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

        var bytes = RptSaldosCajaPdfDocument.Build(
            rows,
            cab,
            "RESUMEN CAJA DIARIO: EFECTIVO = 200.00  YAPE = 85.00  PLIN = 40.00",
            cajaChica: false);
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 1400);
    }

    [Fact]
    public void Resumen_caja_diario_compone_efectivo_y_medios_digitales()
    {
        var c = ResumenCuentaCajaParser.Compose(
            "RESUMEN CAJA DIARIO: EFECTIVO = 200.00  YAPE = 85.00  PLIN = 40.00");
        Assert.Equal(200.00m, c.Efectivo);
        Assert.Equal(125.00m, c.MediosDigitales);
        Assert.Equal(3, c.Items.Count);
        Assert.Equal("EFECTIVO", c.Items[0].Cuenta);
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

    [Fact]
    public void DisplayLabel_distingue_central_y_huanta()
    {
        Assert.Equal("Yape Central", ResumenCuentaCajaParser.DisplayLabel("YAPE CENTRAL"));
        Assert.Equal("Yape Huanta", ResumenCuentaCajaParser.DisplayLabel("YAPE HUANTA"));
        Assert.Equal("Interbank Central", ResumenCuentaCajaParser.DisplayLabel("INTERBANK CENTRAL"));
        Assert.Equal("Interbank Huanta", ResumenCuentaCajaParser.DisplayLabel("INTERBANK HUANTA"));
        Assert.Equal("BCP Central", ResumenCuentaCajaParser.DisplayLabel("BCP CENTRAL"));
        Assert.Equal("BCP Huanta", ResumenCuentaCajaParser.DisplayLabel("BCP HUANTA"));
        Assert.Equal("Efectivo", ResumenCuentaCajaParser.DisplayLabel("EFECTIVO"));
        Assert.Equal("Banco de la Nación", ResumenCuentaCajaParser.DisplayLabel("BANCO DE LA NACION"));
    }

    [Fact]
    public void Compose_aclara_central_cuando_sp_solo_marca_huanta()
    {
        var c = ResumenCuentaCajaParser.Compose(
            "RESUMEN BOVEDA: EFECTIVO = 1181135.76  YAPE = 805774.72  INTERBANK = -353554.02  BCO CREDITO = 98444.39  BCO NACION = 9071.40  YAPE HUANTA = -13531.10  INTERBANK HUANTA = 0.00  BCO CREDITO HUANTA = 0.00");
        Assert.Equal(
            ["EFECTIVO", "YAPE CENTRAL", "INTERBANK CENTRAL", "BCO CREDITO CENTRAL", "BCO NACION", "YAPE HUANTA", "INTERBANK HUANTA", "BCO CREDITO HUANTA"],
            c.Items.Select(i => i.Cuenta).ToArray());
        Assert.Equal("Yape Central", ResumenCuentaCajaParser.DisplayLabel(c.Items[1].Cuenta));
        Assert.Equal("BCP Huanta", ResumenCuentaCajaParser.DisplayLabel(c.Items[^1].Cuenta));
    }

    [Fact]
    public void Movimiento_boveda_con_resumen_central_huanta_es_pdf_valido()
    {
        var cab = new BovedaAbiertaDto(
            7,
            1,
            158331.15m,
            0m,
            42166.20m,
            116164.95m,
            new DateTime(2026, 9, 22, 9, 3, 17),
            null,
            false,
            false);
        var bytes = RptMovimientoBovedaPdfDocument.Build(
            [
                new RptMovimientoBovedaRowDto
                {
                    MovimientoBovedaId = 1,
                    FechaReg = new DateTime(2026, 9, 22, 10, 0, 0),
                    CodOperacion = "TRS",
                    Glosa = "ASIGNACION TEMPORAL: CAJAS",
                    Entrada = 0m,
                    Salida = 5000m,
                    TipoPago = "EFECTIVO",
                    Agente = "Demo",
                },
            ],
            new CredixLegacyReportContext { Oficina = "Oficina Principal", Referencia = "Bóveda N° 7" },
            cab,
            "RESUMEN BOVEDA: EFECTIVO = 1171496.76  YAPE CENTRAL = 798476.72  YAPE HUANTA = -13531.10  INTERBANK CENTRAL = -353714.02  INTERBANK HUANTA = 0.00  BCP CENTRAL = 98174.39  BCP HUANTA = 0.00  BANCO DE LA NACION = 9071.40");
        AssertPdfMagic(bytes);
        Assert.True(bytes.Length > 1500);
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
