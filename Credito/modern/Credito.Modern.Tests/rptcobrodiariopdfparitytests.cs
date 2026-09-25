using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public sealed class RptCobroDiarioPdfParityTests
{
    [Fact]
    public void Ordena_vencidos_antes_que_vigentes_por_fecha_vencimiento()
    {
        var corte = new DateTime(2026, 9, 25);
        var rows = new List<RptCobroDiarioRowDto>
        {
            Row(1, "B", corte.AddDays(5), dias: 0),
            Row(2, "A", corte.AddDays(-10), dias: 10),
            Row(3, "C", corte.AddDays(1), dias: 0),
            Row(4, "D", corte.AddDays(-2), dias: 2),
        };

        var ordered = RptCobroDiarioPdfDocument.OrderLikeLegacy(rows, corte);
        Assert.Equal(new[] { 2, 4, 3, 1 }, ordered.Select(r => r.CreditoId).ToArray());
    }

    [Fact]
    public void Resalta_ultimo_pago_como_legacy()
    {
        var sinPago = Row(1, "X", DateTime.Today, tienePagoReal: false, formaPago: "D");
        var periodico = Row(2, "Y", DateTime.Today, tienePagoReal: true, formaPago: "S");
        var diario = Row(3, "Z", DateTime.Today, tienePagoReal: true, formaPago: "D");

        Assert.Equal("#7030A0", RptCobroDiarioPdfDocument.FechaPagoBackground(sinPago)?.ToString());
        Assert.Equal("#28A745", RptCobroDiarioPdfDocument.FechaPagoBackground(periodico)?.ToString());
        Assert.Null(RptCobroDiarioPdfDocument.FechaPagoBackground(diario));
    }

    [Fact]
    public void Resalta_vencimiento_en_ventana_de_3_dias()
    {
        var corte = new DateTime(2026, 9, 25);
        var hoy = Row(1, "A", corte, dias: 0);
        var en3 = Row(2, "B", corte.AddDays(3), dias: 0);
        var en4 = Row(3, "C", corte.AddDays(4), dias: 0);
        var pasado = Row(4, "D", corte.AddDays(-1), dias: 1);

        Assert.Equal("#FF6347", RptCobroDiarioPdfDocument.FechaVencimientoBackground(hoy, corte)?.ToString());
        Assert.Equal("#FF6347", RptCobroDiarioPdfDocument.FechaVencimientoBackground(en3, corte)?.ToString());
        Assert.Null(RptCobroDiarioPdfDocument.FechaVencimientoBackground(en4, corte));
        Assert.Null(RptCobroDiarioPdfDocument.FechaVencimientoBackground(pasado, corte));
    }

    [Fact]
    public void Celda_pdf_no_usa_elipsis_en_tokens_compactos()
    {
        var size = CredixPdfCellText.FitFontSize("01/07/2026 19:30", 7.5f, softMaxChars: 9);
        Assert.True(size < 7.5f);
        Assert.True(size >= CredixPdfCellText.MinFontSize);
        Assert.True(CredixPdfCellText.IsNonBreakingToken("45555555"));
        Assert.False(CredixPdfCellText.IsNonBreakingToken("ROSADIO GUZMAN"));
    }

    private static RptCobroDiarioRowDto Row(
        int id,
        string cliente,
        DateTime vencimiento,
        int dias = 0,
        bool? tienePagoReal = true,
        string formaPago = "D") =>
        new()
        {
            CreditoId = id,
            Cliente = cliente,
            FechaVencimiento = vencimiento,
            DiasAtrazo = dias,
            FechaPrimerPago = vencimiento.AddMonths(-6),
            TienePagoReal = tienePagoReal,
            FormaPago = formaPago,
            MontoCredito = 100m,
            Interes = 8m,
        };
}
