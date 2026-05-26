using System.Text;
using Credito.Modern.Application.Ventas;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptListaPrecioCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptListaPrecioCsvFormatter.ToUtf8BomCsv(Array.Empty<RptListaPrecioGeneralRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("ArticuloId,TipoArticulo,ArticuloDes,Monto,Descuento,PuntosCanje", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_escapa_comillas_y_comas()
    {
        var rows = new[]
        {
            new RptListaPrecioGeneralRowDto
            {
                ArticuloId = 1,
                TipoArticulo = "A,B",
                ArticuloDes = "Say \"hi\"",
                Monto = 10.5m,
                Descuento = null,
                PuntosCanje = 3,
            },
        };
        var text = Encoding.UTF8.GetString(RptListaPrecioCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("\"A,B\"", text, StringComparison.Ordinal);
        Assert.Contains("\"Say \"\"hi\"\"\"", text, StringComparison.Ordinal);
    }
}
