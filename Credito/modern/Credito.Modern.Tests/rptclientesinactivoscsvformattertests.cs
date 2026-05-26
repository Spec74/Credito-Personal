using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptClientesInactivosCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptClientesInactivosCsvFormatter.ToUtf8BomCsv(Array.Empty<RptClientesInactivosRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("PersonaId,Agente,Codigo", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new List<RptClientesInactivosRowDto>
        {
            new()
            {
                PersonaId = 1205,
                Agente = "Gestor 3",
                Codigo = "C-001",
                Dni = "12345678",
                Cliente = "López, Ana",
                Direccion = "Av. Central 10",
                Celular = "999888777",
                Calificacion = "B",
            },
        };
        var line = DataLine(RptClientesInactivosCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("1205", line);
        Assert.Contains("\"López, Ana\"", line);
        Assert.Contains("Gestor 3", line);
    }
}
