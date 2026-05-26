using System.Text;
using Credito.Modern.Application.CreditoCartera;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class ListarSaldoCarteraCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = ListarSaldoCarteraCsvFormatter.ToUtf8BomCsv(Array.Empty<ListarSaldoCarteraRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("AgenteId,OficinaId,NroDesembolsos", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fecha = new DateTime(2026, 1, 31, 18, 0, 0, DateTimeKind.Unspecified);
        var rows = new List<ListarSaldoCarteraRowDto>
        {
            new()
            {
                AgenteId = 5,
                OficinaId = 1,
                NroDesembolsos = 12,
                MontoDesembolsos = 45000.50m,
                SaldoCartera = 38000m,
                NroClientesSaldoCartera = 40,
                SaldoMoraCartera = 2500.75m,
                NroClientesSaldoMoraCartera = 3,
                SaldoVencido = 800m,
                SaldoMorosidad = 1200m,
                NroClientesNuevos = 2,
                FechaCierre = fecha,
            },
        };
        var line = DataLine(ListarSaldoCarteraCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("5", line);
        Assert.Contains("45000.50", line);
        Assert.Contains("2500.75", line);
    }
}
