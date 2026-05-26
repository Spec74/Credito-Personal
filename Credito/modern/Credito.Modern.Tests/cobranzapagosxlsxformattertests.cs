using ClosedXML.Excel;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;
public sealed class CobranzaPagosXlsxFormatterTests
{
    [Fact]
    public void ToXlsx_produces_valid_zip_xlsx_signature()
    {
        var rows = new List<RptCobroDiarioDetalleRowDto>
        {
            new()
            {
                Nro = 1,
                Cliente = "CLIENTE PRUEBA",
                FormaPago = "D",
                MontoCredito = 500m,
                Interes = 10m,
                MontoTotal = 510m,
                FechaPrimerPago = new DateTime(2024, 1, 15),
                FechaVencimiento = new DateTime(2024, 6, 15),
                TotalPago = 200m,
                Saldo = 310m,
                Pagos = "10.00 (15/01/2024),0.00 (15/02/2024)",
            },
        };

        var bytes = CobranzaPagosXlsxFormatter.ToXlsx(rows, "Gestor Test", "Oficina Test", DateTime.Now);

        Assert.True(bytes.Length > 100);
        Assert.Equal(0x50, bytes[0]); // P
        Assert.Equal(0x4B, bytes[1]); // K

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        Assert.Equal("Cobranza", wb.Worksheet(1).Name);
    }
}
