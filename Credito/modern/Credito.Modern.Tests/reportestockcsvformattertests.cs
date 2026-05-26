using System.Globalization;
using System.Text;
using Credito.Modern.Application.Almacenes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public sealed class ReporteStockCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = ReporteStockCsvFormatter.ToUtf8BomCsv(Array.Empty<ReporteStockRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Nro,TipoArticulo,ArticuloId,Articulo,Stock,Series", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new[]
        {
            new ReporteStockRowDto(
                Nro: 1,
                TipoArticulo: "TEL",
                ArticuloId: 42,
                Articulo: "Celular X",
                Stock: 5,
                Series: "SN-001"),
        };
        var line = DataLine(ReporteStockCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Equal("1,TEL,42,Celular X,5,SN-001", line);
    }
}
