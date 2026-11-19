using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class MediosDigitalesConciliacionTests
{
    [Fact]
    public void Yape_parser_filtra_TE_PAGO()
    {
        var csv = """
            Tipo de Transacción;Origen;Monto;Mensaje;Fecha de operación
            TE PAGÓ;Leide Her*;208.00;mego campos;16/11/2026 18:05:46
            PAGASTE;RENE MANTILLA;50.00;;16/11/2026 17:00:00
            TE PAGÓ;Franco Bej*;600.00;;16/11/2026 08:24:18
            """;

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var parsed = YapeReporteParser.Parse(ms, "yape.csv");

        Assert.Equal(3, parsed.FilasArchivo);
        Assert.Equal(1, parsed.FilasPagasteDescartadas);
        Assert.Equal(2, parsed.Ingresos.Count);
        Assert.All(parsed.Ingresos, i => Assert.Equal("Yape", i.Canal));
    }

    [Fact]
    public void Matcher_auto_solo_unicos()
    {
        var ingresos = new[]
        {
            new MedioDigitalReporteMovimiento(1, "Yape", "Familiar X*", 100.00m, "", new DateTime(2026, 11, 16, 10, 15, 33), "16/11/2026 10:15:33"),
            new MedioDigitalReporteMovimiento(2, "Yape", "Otro*", 50.00m, "", new DateTime(2026, 11, 16, 11, 0, 1), "16/11/2026 11:00:01"),
            new MedioDigitalReporteMovimiento(3, "Yape", "Dup1*", 20.00m, "", new DateTime(2026, 11, 16, 12, 0, 0), "16/11/2026 12:00:00"),
            new MedioDigitalReporteMovimiento(4, "Yape", "Dup2*", 20.00m, "", new DateTime(2026, 11, 16, 12, 0, 40), "16/11/2026 12:00:40"),
        };

        var pendientes = new[]
        {
            Row(101, "CLIENTE", 100m, "16/11/2026 10:15", "YAPE"),
            Row(102, "OTRO", 50m, "16/11/2026 11:01", "YAPE"),
            Row(103, "DUP A", 20m, "16/11/2026 12:00", "YAPE"),
            Row(104, "DUP B", 20m, "16/11/2026 12:00", "YAPE"),
        };

        var result = MedioDigitalConciliacionMatcher.Match("Yape", ingresos, pendientes, 4, 0);

        Assert.Equal(1, result.Auto);
        Assert.Contains(result.Pares, p => p.MovimientoCajaId == 101 && p.Estado == nameof(MedioDigitalMatchEstado.Auto));
        Assert.Contains(result.Pares, p => p.MovimientoCajaId == 102 && p.Estado == nameof(MedioDigitalMatchEstado.Sugerido));
        Assert.True(result.Ambiguos >= 2);
    }

    [Fact]
    public void Plin_parser_extrae_filas_interbank()
    {
        var ocr = """
            Millonaria Soles
            S/ 1,967.63
            Movimientos
            Plin-mario
            Mar 17 Nov | 9:24 am
            S/ 500.00
            Plin-emerson
            Mar 17 Nov | 8:59 am
            S/ 7.00
            Yape-elberto
            Mar 17 Nov | 8:49 am
            S/ 300.00
            """;

        var parsed = PlinInterbankParser.ParseOcrText(ocr, "shot1.png", new DateTime(2026, 11, 17), soloPlin: true);

        Assert.Equal(2, parsed.Ingresos.Count);
        Assert.Contains(parsed.Ingresos, i => i.Origen.Contains("mario", StringComparison.OrdinalIgnoreCase) && i.Monto == 500m);
        Assert.Contains(parsed.Ingresos, i => i.Origen.Contains("emerson", StringComparison.OrdinalIgnoreCase) && i.Monto == 7m);
        Assert.All(parsed.Ingresos, i => Assert.Equal("Plin", i.Canal));
        Assert.DoesNotContain(parsed.Ingresos, i => i.Origen.Contains("elberto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plin_merge_deduplica_capturas_solapadas()
    {
        var a = PlinInterbankParser.ParseOcrText(
            "Plin-mario\nMar 17 Nov | 9:24 am\nS/ 500.00",
            "a.png",
            new DateTime(2026, 11, 17));
        var b = PlinInterbankParser.ParseOcrText(
            "Plin-mario\nMar 17 Nov | 9:24 am\nS/ 500.00\nPlin-emerson\nMar 17 Nov | 8:59 am\nS/ 7.00",
            "b.png",
            new DateTime(2026, 11, 17));

        var merged = PlinInterbankParser.Merge([a, b]);
        Assert.Equal(2, merged.Ingresos.Count);
    }

    [Fact]
    public void Tipo_compatible_yape_plin()
    {
        Assert.True(MedioDigitalConciliacionMatcher.EsTipoPagoCompatible("Yape Gerente", "Yape"));
        Assert.True(MedioDigitalConciliacionMatcher.EsTipoPagoCompatible("PLIN", "Plin"));
        Assert.False(MedioDigitalConciliacionMatcher.EsTipoPagoCompatible("Yape", "Plin"));
    }

    private static PagosNoVerificadosRowDto Row(
        int id, string cliente, decimal monto, string fecha, string tipo) =>
        new()
        {
            MovimientoCajaId = id,
            Cliente = cliente,
            ImportePago = monto,
            FechaTransferencia = fecha,
            TipoPago = tipo,
        };
}
