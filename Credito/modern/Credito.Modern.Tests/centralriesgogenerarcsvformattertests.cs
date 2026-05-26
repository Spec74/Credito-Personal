using System.Text;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

using static Credito.Modern.Tests.CsvTestHelpers;

public class CentralRiesgoGenerarCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_prefija_bom_y_cabecera()
    {
        var bytes = CentralRiesgoGenerarCsvFormatter.ToUtf8BomCsv(Array.Empty<CentralRiesgoGenerarRowDto>());
        AssertUtf8Bom(bytes);
        Assert.StartsWith("Anio,Mes,CreditoId,Periodo", HeaderLine(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void ToUtf8BomCsv_formatea_fila()
    {
        var rows = new List<CentralRiesgoGenerarRowDto>
        {
            new()
            {
                Anio = 2026,
                Mes = 5,
                CreditoId = 1001,
                Periodo = "202605",
                Entidad = "ENT-01",
                TipoDoc = 1,
                NumDoc = "12345678",
                RazonSocial = null,
                ApePat = "Gómez",
                ApeMat = "López",
                Nombres = "Carlos",
                TipoPersona = 1,
                ModalidadCredito = 2,
                DeudaMenor30 = "100.50",
                DeudaMayor30 = "0",
                Calificacion = 3,
                DiasAtrazo = 15,
                Direccion = "Av. Lima 100",
                celular = "999111222",
            },
        };
        var line = DataLine(CentralRiesgoGenerarCsvFormatter.ToUtf8BomCsv(rows));
        Assert.Contains("1001", line);
        Assert.Contains("Gómez", line);
        Assert.Contains("999111222", line);
    }
}
