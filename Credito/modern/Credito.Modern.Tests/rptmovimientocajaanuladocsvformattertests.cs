using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class RptMovimientoCajaAnuladoCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = RptMovimientoCajaAnuladoCsvFormatter.ToUtf8BomCsv(Array.Empty<RptMovimientoCajaAnuladoRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("MovimientoCajaId,Operacion,ImportePago", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var fechaReg = new DateTime(2026, 1, 10, 14, 30, 0, DateTimeKind.Unspecified);
        var rows = new List<RptMovimientoCajaAnuladoRowDto>
        {
            new()
            {
                MovimientoCajaId = 55,
                Operacion = "PAGO",
                ImportePago = 125.50m,
                Persona = "García, Juan",
                Descripcion = "Anulado por error",
                FechaReg = fechaReg,
                UsuarioRegistro = "admin",
                MotivoAnulacion = "Duplicado",
                FechaAnulacion = fechaReg.AddHours(2),
                UsuarioAnulacion = "supervisor",
            },
        };
        var line = DataLine(RptMovimientoCajaAnuladoCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("55", line);
        Assert.Contains("125.50", line);
        Assert.Contains("\"García, Juan\"", line);
    }
}
