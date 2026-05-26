using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptClientesBloqueadosCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptClientesBloqueadosCsvFormatter.ToUtf8BomCsv(Array.Empty<RptClientesBloqueadosRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Agente,NumeroDocumento,Cliente,Direccion,DireccionRef,Celular,Calificacion,Nota", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new List<RptClientesBloqueadosRowDto>
        {
            new()
            {
                Agente = "Gestor 1",
                NumeroDocumento = "12345678",
                Cliente = "Pérez, Ana",
                Direccion = "Av. 1",
                Celular = "999",
                Calificacion = "B",
                Nota = "Bloqueo temporal",
            },
        };
        var line = DataLine(RptClientesBloqueadosCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("\"Pérez, Ana\"", line);
        Assert.Contains("12345678", line);
    }
}
