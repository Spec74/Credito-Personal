using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public class CentralRiesgoGenerarTxtFormatterTests
{
    [Fact]
    public void FormatLine_usa_padding_legacy()
    {
        var row = new CentralRiesgoGenerarRowDto
        {
            Periodo = "202501",
            Entidad = "01",
            TipoDoc = 1,
            NumDoc = "12345678",
            RazonSocial = "ACME",
            ApePat = "PEREZ",
            ApeMat = "LOPEZ",
            Nombres = "JUAN",
            TipoPersona = 1,
            ModalidadCredito = 2,
            DeudaMenor30 = "100.50",
            DeudaMayor30 = "0.00",
            Calificacion = 1,
            DiasAtrazo = 0,
            Direccion = "CALLE 1",
            celular = "999888777",
        };

        var bytes = CentralRiesgoGenerarTxtFormatter.ToUtf8Bytes([row]);
        var line = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd();

        Assert.Contains("20250101", line);
        Assert.Contains("12345678", line);
        Assert.Contains("10050", line);
        Assert.DoesNotContain("100.50", line);
    }
}
