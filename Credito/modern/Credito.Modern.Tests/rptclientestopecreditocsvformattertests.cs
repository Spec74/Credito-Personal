using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptClientesTopeCreditoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptClientesTopeCreditoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptClientesTopeCreditoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Agente,NumeroDocumento,Cliente,Direccion,DireccionRef,Celular,Calificacion,TopeCredito,Nota", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila_con_tope()
    {
        var rows = new List<RptClientesTopeCreditoRowDto>
        {
            new()
            {
                Agente = "Gestor 2",
                NumeroDocumento = "87654321",
                Cliente = "López, Luis",
                TopeCredito = 2500.5m,
                Nota = "Tope vigente",
            },
        };
        var line = DataLine(RptClientesTopeCreditoCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("\"López, Luis\"", line);
        Assert.Contains("2500.5", line);
        Assert.EndsWith("Tope vigente", line);
    }
}
