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
        var header = HeaderLine(bytes);
        Assert.StartsWith("PersonaId,Agente,Codigo", header, StringComparison.Ordinal);
        Assert.Contains("TopeCredito", header, StringComparison.Ordinal);
        Assert.Contains("ClasificacionRiesgoSBS", header, StringComparison.Ordinal);
        Assert.Contains("Depurado", header, StringComparison.Ordinal);
        Assert.Contains("MontoCredito", header, StringComparison.Ordinal);
        Assert.Contains("FechaCancelacion", header, StringComparison.Ordinal);
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
                MontoCredito = 500m,
                TopeCredito = 2000m,
                FechaCancelacion = new DateTime(2026, 6, 15),
                TotalCreditos = 6,
                DiasInactividad = 90,
                Depurado = "NO",
                ClasificacionRiesgoSBS = "NORMAL",
            },
        };
        var line = DataLine(RptClientesInactivosCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("1205", line);
        Assert.Contains("\"López, Ana\"", line);
        Assert.Contains("Gestor 3", line);
        Assert.Contains("2000", line);
        Assert.Contains("NORMAL", line);
        Assert.Contains("2026-06-15", line);
    }
}
