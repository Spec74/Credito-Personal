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
    public void Build_mixto_con_medios_devuelve_pdf()
    {
        var dto = new MovimientoCajaTicketDto(
            8,
            MovimientoCajaTicketLayout.CuotaCredito,
            8,
            "GUILLEN RAMOS, RAYDA ANTONIA",
            "FPAREDES",
            new DateTime(2026, 11, 25, 7, 14, 0),
            "Pichari",
            "CREDI FACIL CREDITO 2",
            "PAGO MIXTO",
            string.Empty,
            250m,
            2,
            333.33m,
            166.67m,
            10m,
            0m,
            0m,
            null,
            null,
            250m,
            125m,
            "2 de 3",
            null,
            0,
            0m,
            "CANCELADO",
            530m,
            MedioPago: "Yape S/ 125.00 + Plin S/ 125.00",
            MediosPago:
            [
                new MovimientoCajaTicketMedioDto(7, "Yape", 125m),
                new MovimientoCajaTicketMedioDto(8, "Plin", 125m),
            ]);

        Assert.Equal(2, dto.MediosPago!.Count);
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
