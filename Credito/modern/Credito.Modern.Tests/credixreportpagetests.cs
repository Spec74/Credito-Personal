using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Reportes;
using QuestPDF.Fluent;

namespace Credito.Modern.Tests;

public class CredixReportPageTests
{
    [Fact]
    public void Build_genera_pdf_con_meta_y_firmas()
    {
        var pdf = CredixReportPage.Build(
            "REPORTE PRUEBA ESTÁNDAR",
            [("Oficina", "Pichari"), ("Estado", "ABIERTO")],
            col =>
            {
                col.Item().Text("Cuerpo de prueba");
            },
            new CredixReportPage.Options { FooterRowCount = 1 });

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 400);
        Assert.Equal(0x25, pdf[0]); // %PDF
        Assert.Equal((byte)'P', pdf[1]);
    }

    [Fact]
    public void Build_rechaza_titulo_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            CredixReportPage.Build(
                "  ",
                [],
                col => col.Item().Text("x")));
    }

    [Fact]
    public void Movimiento_credito_usa_shell_estandar()
    {
        var cab = new RptEstadoCreditoCabeceraDto(
            1, 1, "CREDI FACIL",
            new DateTime(2026, 11, 7), new DateTime(2026, 12, 7),
            1000m, "DIARIO", 3, 6m, "DES", "C1",
            "CLIENTE PRUEBA", "ANALISTA", "DESEMBOLSO",
            10m, 0m, "ADE", 1060m, 1000m);

        var pdf = RptMovimientoCreditoFichaPdfDocument.Build(
            cab,
            1,
            [
                new RptMovimientoCreditoRowDto
                {
                    MovimientoCajaId = 0,
                    Fecha = new DateTime(2026, 11, 24),
                    Operacion = "---",
                    Glosa = "SALDO INICIAL",
                    ImportePago = 0m,
                    Saldo = 1060m,
                },
            ]);

        Assert.True(pdf.Length > 500);
    }
}
