using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public sealed class CredixLegacyReportCatalogTests
{
    [Theory]
    [InlineData("Movimiento bóveda", CredixLegacyReportKey.MovimientoBoveda)]
    [InlineData("Movimiento boveda", CredixLegacyReportKey.MovimientoBoveda)]
    [InlineData("MOVIMIENTO BÓVEDA", CredixLegacyReportKey.MovimientoBoveda)]
    [InlineData("Movimiento crédito", CredixLegacyReportKey.MovimientoCredito)]
    [InlineData("Saldo cartera", CredixLegacyReportKey.ListarSaldoCartera)]
    [InlineData("Avales", CredixLegacyReportKey.AvalPersona)]
    [InlineData("Central de riesgo", CredixLegacyReportKey.CentralRiesgoGenerar)]
    [InlineData("Morosidad crédito", CredixLegacyReportKey.CreditoMorosidad)]
    [InlineData("Rentabilidad crédito", CredixLegacyReportKey.CreditoRentabilidad)]
    [InlineData("Aprobación crédito", CredixLegacyReportKey.CreditoAprobacion)]
    [InlineData("Morosos pagados", CredixLegacyReportKey.CreditosMorososPagados)]
    [InlineData("Clientes nuevos del mes", CredixLegacyReportKey.ClientesNuevosMes)]
    [InlineData("Lista de precios", CredixLegacyReportKey.ListaPrecio)]
    [InlineData("Créditos observados", CredixLegacyReportKey.CreditoObservado)]
    [InlineData("Créditos condonados", CredixLegacyReportKey.CreditoCondonado)]
    public void Titulos_de_la_api_resuelven_el_catalogo(string title, CredixLegacyReportKey expected)
    {
        Assert.True(CredixLegacyReportCatalog.TryGetByLegacyTitle(title, out var key));
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Titulo_desconocido_no_fuerza_un_informe()
    {
        Assert.False(CredixLegacyReportCatalog.TryGetByLegacyTitle("Test sep", out _));
    }

    [Fact]
    public void Movimiento_boveda_pdf_usa_layout_vertical_por_pocas_columnas()
    {
        var rows = new[]
        {
            new RptMovimientoBovedaRowDto
            {
                MovimientoBovedaId = 757255,
                FechaReg = new DateTime(2026, 9, 10, 23, 20, 34),
                CodOperacion = "TRF",
                Glosa = "TRF. SALIDA: NA",
                Salida = 1000m,
                TipoPago = "YAPE",
                Agente = "ADMIN",
            },
        };
        var csv = RptMovimientoBovedaCsvFormatter.ToUtf8BomCsv(rows);
        var pdf = TabularPdfDocument.FromUtf8BomCsv("Movimiento bóveda", csv);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
        Assert.True(pdf.Length > 800);
        var def = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.MovimientoBoveda);
        Assert.False(def.Landscape);
        Assert.False(CredixLegacyPdfDocument.EffectiveLandscape(def.Columns.Count, def.Landscape));
    }

    [Theory]
    [InlineData(5, true, true)]
    [InlineData(5, false, false)]
    [InlineData(7, false, false)]
    [InlineData(8, true, true)]
    [InlineData(8, false, true)]
    [InlineData(10, true, true)]
    [InlineData(15, true, true)]
    [InlineData(20, false, true)]
    public void Orientacion_efectiva_segun_columnas(int cols, bool prefer, bool expected) =>
        Assert.Equal(expected, CredixLegacyPdfDocument.EffectiveLandscape(cols, prefer));

    [Fact]
    public void Creditos_aprobados_sin_columna_desembolso_duplicada()
    {
        var cols = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.CreditoAprobacion).Columns;
        Assert.DoesNotContain(cols, c => c.CsvName == "MontoDesembolso");
        Assert.Contains(cols, c => c.CsvName == "MontoCredito");
        Assert.True(CredixLegacyReportCatalog.Get(CredixLegacyReportKey.CreditoAprobacion).Landscape);
    }

    [Fact]
    public void Formato_de_celda_fecha_e_importe()
    {
        var money = new CredixLegacyColumnSpec("Entrada", "Entrada", CredixColumnAlign.Right, 1);
        Assert.Equal("10/09/2026 23:20", CredixLegacyPdfDocument.FormatDisplayCell("2026-09-10 23:20:34", null));
        Assert.Equal("1,000.00", CredixLegacyPdfDocument.FormatDisplayCell("1000.00", money));
    }

    [Theory]
    [InlineData("Nota", CredixColumnAlign.Left)]
    [InlineData("Negocio", CredixColumnAlign.Left)]
    [InlineData("Cliente", CredixColumnAlign.Left)]
    [InlineData("Monto crédito", CredixColumnAlign.Right)]
    [InlineData("Saldo", CredixColumnAlign.Right)]
    [InlineData("% cobro", CredixColumnAlign.Right)]
    [InlineData("N° créd.", CredixColumnAlign.Center)]
    [InlineData("Fecha", CredixColumnAlign.Center)]
    [InlineData("DNI", CredixColumnAlign.Center)]
    [InlineData("Estado", CredixColumnAlign.Center)]
    public void Alineacion_inferida_es_profesional(string header, CredixColumnAlign expected)
    {
        Assert.Equal(expected, CredixLegacyPdfDocument.GuessAlign(header));
    }

    [Fact]
    public void Observados_y_condonados_no_imprimen_ids_tecnicos()
    {
        var observado = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.CreditoObservado).Columns;
        Assert.DoesNotContain(observado, c => c.CsvName is "OficinaId" or "AgenteId");
        Assert.Contains(observado, c => c.CsvName == "CreditoId" && c.Align == CredixColumnAlign.Center);

        var condonado = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.CreditoCondonado).Columns;
        Assert.DoesNotContain(condonado, c => c.CsvName is "OficinaId" or "AgenteId");
        Assert.Contains(condonado, c => c.CsvName == "CreditoId");
    }

    [Fact]
    public void Clientes_nuevos_alineado_con_observados()
    {
        var cols = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.ClientesNuevosMes).Columns;
        Assert.DoesNotContain(cols, c => c.CsvName is "OficinaId" or "AgenteId");
        Assert.Contains(cols, c => c.CsvName == "CreditoId");
        Assert.Contains(cols, c => c.CsvName == "Cliente");
        Assert.Contains(cols, c => c.CsvName == "TramiteAdm");
        Assert.Contains(cols, c => c.CsvName == "CentralRiesgo");
    }

    [Fact]
    public void Inactivos_no_imprimen_persona_id()
    {
        var cols = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.ClientesInactivos).Columns;
        Assert.DoesNotContain(cols, c => c.CsvName == "PersonaId");
        Assert.DoesNotContain(cols, c => c.CsvName == "Codigo");
        Assert.Contains(cols, c => c.CsvName == "Dni" && c.Align == CredixColumnAlign.Center);
        Assert.Contains(cols, c => c.CsvName == "Direccion");
        Assert.Contains(cols, c => c.CsvName == "DireccionRef");
        Assert.Contains(cols, c => c.CsvName == "DireccionNegocio");
        Assert.Contains(cols, c => c.CsvName == "DireccionNegocioRef");
        Assert.Contains(cols, c => c.CsvName == "TopeCredito");
        Assert.Contains(cols, c => c.CsvName == "ClasificacionRiesgoSBS");
        Assert.Contains(cols, c => c.CsvName == "Depurado");
        Assert.Contains(cols, c => c.CsvName == "MontoCredito");
        Assert.Contains(cols, c => c.CsvName == "TotalCreditos");
        Assert.Contains(cols, c => c.CsvName == "FechaCancelacion");
    }

    [Fact]
    public void Encabezado_completa_oficina_desde_las_filas()
    {
        var merged = CredixLegacyPdfDocument.MergeMetadata(
            Array.Empty<CredixLegacyPdfDocument.MetadataLine>(),
            ["Oficina", "Cliente"],
            [["Central", "Ana"], ["Central", "Luis"]]);
        Assert.Contains(merged, m => m.Label.StartsWith("Oficina", StringComparison.OrdinalIgnoreCase) && m.Value == "Central");
    }

    [Fact]
    public void Ancho_de_columna_prioriza_el_contenido()
    {
        var headers = new[] { "N° créd.", "Cliente" };
        var rows = new[] { new[] { "12", "CLIENTE CON NOMBRE MUY LARGO PARA IMPRESION" } };
        var weights = CredixLegacyPdfDocument.ComputeContentWeights(headers, rows, null);
        Assert.True(weights[1] > weights[0]);
    }

    [Fact]
    public void Columna_DNI_tiene_piso_suficiente_para_ocho_digitos()
    {
        var headers = new[] { "DNI", "Cliente", "Monto" };
        var rows = new[] { new[] { "45555555", "ANA", "100.00" } };
        var weights = CredixLegacyPdfDocument.ComputeContentWeights(headers, rows, null);
        Assert.True(weights[0] >= 1.15f, $"DNI weight={weights[0]}");
        Assert.True(CredixPdfCellText.IsNonBreakingToken("45555555"));
        Assert.False(CredixPdfCellText.IsNonBreakingToken("NOMBRE COMPLETO LARGO"));
    }

    [Fact]
    public void Metadatos_de_periodo_usan_una_sola_linea()
    {
        var lines = CredixLegacyReportCatalog.BuildMetadata(
            CredixLegacyReportKey.ReporteCredito,
            new CredixLegacyReportContext
            {
                Oficina = "Oficina 1",
                Agente = "TODOS",
                FechaIni = "01/09/2026",
                FechaFin = "12/09/2026",
                Estado = "DES",
            });
        Assert.Contains(lines, l => l.Label.StartsWith("Periodo", StringComparison.Ordinal) && l.Value.Contains("01/09/2026"));
        Assert.Contains(lines, l => l.Label.StartsWith("Oficina", StringComparison.Ordinal) && l.Value == "Oficina 1");
        Assert.DoesNotContain(lines, l => l.Label.StartsWith("Desde", StringComparison.Ordinal));
    }
}
