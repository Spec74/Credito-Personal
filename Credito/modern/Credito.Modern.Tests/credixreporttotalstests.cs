using Credito.Modern.Application.Reportes;

namespace Credito.Modern.Tests;

public class CredixReportTotalsTests
{
    private static readonly CredixLegacyColumnSpec[] Columns =
    [
        new("Caja", "Caja"),
        new("SaldoInicial", "Saldo ini.", CredixColumnAlign.Right),
        new("Entradas", "Entradas", CredixColumnAlign.Right),
        new("Resumen", "Resumen"),
    ];

    [Fact]
    public void Compute_suma_solo_las_columnas_declaradas()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "SAN JOSE", "100.00", "1065.00", "EFECTIVO = 1165" },
            new[] { "HUANTA", "900.00", "1644.65", "EFECTIVO = 2544.65" },
        };

        var totals = CredixReportTotals.Compute(Columns, rows, ["SaldoInicial", "Entradas"]);

        Assert.Equal(2, totals.Count);
        Assert.Equal(1000m, totals[1]);
        Assert.Equal(2709.65m, totals[2]);
    }

    [Fact]
    public void Compute_ignora_celdas_no_numericas_y_faltantes()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "SAN JOSE", "", "50" },
            new[] { "HUANTA", "—", "50" },
            new[] { "SOLO UNA CELDA" },
        };

        var totals = CredixReportTotals.Compute(Columns, rows, ["SaldoInicial", "Entradas"]);

        Assert.Equal(0m, totals[1]);
        Assert.Equal(100m, totals[2]);
    }

    [Fact]
    public void Compute_sin_declaracion_no_totaliza()
    {
        var rows = new List<IReadOnlyList<string>> { new[] { "SAN JOSE", "100", "10", "" } };

        Assert.Empty(CredixReportTotals.Compute(Columns, rows, null));
        Assert.Empty(CredixReportTotals.Compute(Columns, rows, []));
        Assert.Empty(CredixReportTotals.Compute(Columns, [], ["SaldoInicial"]));
        Assert.Empty(CredixReportTotals.Compute(Columns, rows, ["NoExiste"]));
    }

    [Fact]
    public void Catalogo_totaliza_los_importes_de_los_informes_de_caja()
    {
        // Paridad rptCajasAsignadas.rdlc: totaliza los cuatro importes, entradas antes de salidas.
        var cajasAsignadas = CredixLegacyReportCatalog.Get(CredixLegacyReportKey.CajasAsignadas);
        Assert.Equal(
            ["SaldoInicial", "Entradas", "Salidas", "SaldoFinal"],
            cajasAsignadas.TotalColumns);
        Assert.Equal(
            ["CajaDiarioId", "Caja", "Modo", "Cajero", "FechaIniOperacion", "FechaFinOperacion",
             "SaldoInicial", "Entradas", "Salidas", "SaldoFinal", "Resumen"],
            cajasAsignadas.Columns.Select(c => c.CsvName));
        Assert.Equal(
            ["ImportePago"],
            CredixLegacyReportCatalog.Get(CredixLegacyReportKey.SaldosCaja).TotalColumns);
        Assert.Equal(
            ["Entrada", "Salida"],
            CredixLegacyReportCatalog.Get(CredixLegacyReportKey.MovimientoBoveda).TotalColumns);
    }
}
