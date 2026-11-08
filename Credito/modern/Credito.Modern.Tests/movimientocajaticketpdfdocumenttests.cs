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

        AssertPdf(MovimientoCajaTicketPdfDocument.Build(dto));
    }

    [Fact]
    public void Build_boveda_con_medio_entrada_y_titulo_devuelve_pdf()
    {
        var dto = new MovimientoCajaTicketDto(
            21,
            MovimientoCajaTicketLayout.Simple,
            0,
            "BÓVEDA",
            "René Pérez",
            new DateTime(2026, 11, 8, 11, 0, 0),
            "OFICINA PRINCIPAL",
            "Operación TRE (Activo)",
            "Entrada",
            "transferencia Interbank sucursal",
            150m,
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
            null,
            null,
            MedioPago: "Interbank Gerente",
            TituloComprobante: "COMPROBANTE DE BÓVEDA",
            Entrada: 150m,
            Salida: null);

        Assert.Equal("Interbank Gerente", dto.MedioPago);
        Assert.Equal("COMPROBANTE DE BÓVEDA", dto.TituloComprobante);
        Assert.Equal(150m, dto.Entrada);
        AssertPdf(MovimientoCajaTicketPdfDocument.Build(dto));
    }

    private static void AssertPdf(byte[] bytes)
    {
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 200);
        Assert.Equal(0x25, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
