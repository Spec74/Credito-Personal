using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class RptComprobantesCajaChicaCsvFormatterTests
{
    [Fact]
    public void ToUtf8BomCsv_incluye_encabezado_y_fila()
    {
        var rows = new[]
        {
            new RptComprobantesCajaChicaRowDto
            {
                Gasto = "Gasto oficina",
                Fecha = new DateTime(2026, 1, 15),
                Documento = "FACTURA",
                Serie = "F001",
                Numero = "123",
                Ruc = "20123456789",
                RazonSocial = "Proveedor SA",
                DetalleGasto = "Papelería",
                Importe = 50.5m,
            },
        };

        var bytes = RptComprobantesCajaChicaCsvFormatter.ToUtf8BomCsv(rows);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("Gasto,Fecha,Documento", text);
        Assert.Contains("20123456789", text);
        Assert.Contains("50.5", text);
    }
}
