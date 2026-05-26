using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class MovimientoCajaTicketPdfDocumentTests
{
    [Fact]
    public void Build_simple_devuelve_pdf_no_vacio()
    {
        var dto = new MovimientoCajaTicketDto(
            1001,
            MovimientoCajaTicketLayout.Simple,
            42,
            "Cliente Prueba",
            "cajero1",
            new DateTime(2026, 5, 18, 10, 30, 0),
            "Oficina Central",
            "Producto X",
            "PAGO INICIAL",
            string.Empty,
            150.5m,
            99,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        var bytes = MovimientoCajaTicketPdfDocument.Build(dto);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 200);
        Assert.Equal(0x25, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
