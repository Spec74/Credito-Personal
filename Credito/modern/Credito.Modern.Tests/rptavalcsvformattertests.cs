using System.Text;using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptAvalCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptAvalCsvFormatter.ToUtf8BomCsv(Array.Empty<RptAvalRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Grupo,CreditoId,MontoCredito,Estado,Persona,Dni,Celular", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new List<RptAvalRowDto>
        {
            new()
            {
                Grupo = "G1",
                CreditoId = 42,
                MontoCredito = 1500m,
                Estado = "ACT",
                Persona = "Juan",
                Dni = "12345678",
                Celular = "999",
            },
        };
        var line = DataLine(RptAvalCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Equal(string.Join(',', "G1", "42", "1500", "ACT", "Juan", "12345678", "999"), line);
    }
}
