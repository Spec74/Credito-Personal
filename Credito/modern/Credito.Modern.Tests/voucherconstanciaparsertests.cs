using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public sealed class VoucherConstanciaParserTests
{
    [Fact]
    public void Plin_constancia_extrae_monto_fecha_codigo()
    {
        const string ocr = """
            Constancia
            plin
            ¡Pago exitoso!
            S/ 250.00
            Regresa a la ventana del comercio para finalizar tu compra
            Comercio
            Shopstar
            #123456789012345678
            Comisión GRATIS
            Fecha y hora
            01 Mar 2022 10:34 AM
            Código de Operación
            00051136547657787543
            """;

        var r = VoucherConstanciaParser.Parse(ocr, montoEsperado: 250m);
        Assert.True(r.Ok);
        Assert.Equal("Plin", r.CanalDetectado);
        Assert.Equal(250m, r.Monto);
        Assert.True(r.CoincideMonto);
        Assert.Equal(new DateTime(2022, 3, 1, 10, 34, 0), r.FechaOperacion);
        Assert.Equal("00051136547657787543", r.CodigoOperacion);
    }

    [Fact]
    public void Yape_te_yapearon_extrae_campos()
    {
        const string ocr = """
            ¡Te Yapearon!
            S/ 20
            Ruth Dur*
            21 nov. 2026
            08:46 p. m.
            Código de Seguridad
            4 1 1
            Nro. de celular *** *** 870
            Destino Yape
            Nro. de operación 20758411
            """;

        var r = VoucherConstanciaParser.Parse(ocr, 20m);
        Assert.True(r.Ok);
        Assert.Equal("Yape", r.CanalDetectado);
        Assert.Equal(20m, r.Monto);
        Assert.Equal(new DateTime(2026, 11, 21, 20, 46, 0), r.FechaOperacion);
        Assert.Equal("20758411", r.CodigoOperacion);
    }

    [Fact]
    public void Bcp_transferencia_extrae_fecha_larga()
    {
        const string ocr = """
            BCP
            ¡Transferencia exitosa!
            S/ 1.00
            Jueves, 26 Noviembre 2026 - 08:24 a.m.
            Desde Ahorro Soles **** 6088
            Enviado a Cuenta Digital Soles **** 9013
            Número de operación 00800868
            """;

        var r = VoucherConstanciaParser.Parse(ocr, 1m);
        Assert.True(r.Ok);
        Assert.Equal("BCP", r.CanalDetectado);
        Assert.Equal(1m, r.Monto);
        Assert.Equal(new DateTime(2026, 11, 26, 8, 24, 0), r.FechaOperacion);
        Assert.Equal("00800868", r.CodigoOperacion);
    }

    [Fact]
    public void Interbank_pagaste_y_plineo()
    {
        const string pagaste = """
            Interbank
            Pagaste
            S/ 300.00
            a UNIVERSIDAD NACIONAL DE TRUJILLO
            Fecha y hora 30 Jul 2024 11:13 PM
            Código de operación 7348946
            """;
        var a = VoucherConstanciaParser.Parse(pagaste, 300m);
        Assert.True(a.Ok);
        Assert.Equal("Interbank", a.CanalDetectado);
        Assert.Equal(new DateTime(2024, 7, 30, 23, 13, 0), a.FechaOperacion);

        const string plineo = """
            Interbank
            Plineo exitoso
            S/ 50
            14 Nov 2026 08:11 AM
            Enviado a Evelyn Perez Q
            988 667 248 - Yape
            Código de operación 41460846
            """;
        var b = VoucherConstanciaParser.Parse(plineo, 50m);
        Assert.True(b.Ok);
        Assert.Equal("Plin", b.CanalDetectado); // Plineo → Plin
        Assert.Equal(new DateTime(2026, 11, 14, 8, 11, 0), b.FechaOperacion);
    }

    [Fact]
    public void Banco_nacion_fecha_slash()
    {
        const string ocr = """
            ¡Gracias, Lino! La operación fue realizada con éxito
            Nro. operación 0007764
            26/04/2024 15:52:00
            S/ 25.55
            Entidad destino Banco de la Nación
            Beneficiario ****
            Nro. celular *** *** 118
            Comisión Gratis
            """;

        var r = VoucherConstanciaParser.Parse(ocr, 25.55m);
        Assert.True(r.Ok);
        Assert.Equal("BancoNacion", r.CanalDetectado);
        Assert.Equal(25.55m, r.Monto);
        Assert.Equal(new DateTime(2024, 4, 26, 15, 52, 0), r.FechaOperacion);
        Assert.Equal("0007764", r.CodigoOperacion);
    }

    [Fact]
    public void Monto_no_coincide_advierte()
    {
        const string ocr = """
            plin ¡Pago exitoso! S/ 100.00
            Fecha y hora 01 Mar 2022 10:34 AM
            Código de Operación 12345678901
            """;
        var r = VoucherConstanciaParser.Parse(ocr, 250m);
        Assert.True(r.Ok);
        Assert.False(r.CoincideMonto);
        Assert.Contains(r.Advertencias, w => w.Contains("no coincide", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Prefiere_monto_esperado_entre_varios()
    {
        const string ocr = """
            ¡Gracias! La operación fue realizada con éxito
            Nro. operación 0000139
            28/11/2023 10:59:59
            Monto pagado S/ 525.00
            Comisión S/ 1.00
            Cargo en cuenta S/ 526.00
            Banco de la Nación
            """;
        var r = VoucherConstanciaParser.Parse(ocr, 525m);
        Assert.True(r.Ok);
        Assert.Equal(525m, r.Monto);
        Assert.True(r.CoincideMonto);
        Assert.Equal("BancoNacion", r.CanalDetectado);
    }

    [Fact]
    public void Transferencia_diferida_advierte()
    {
        const string ocr = """
            Interbank
            Transferiste
            S/ 3800.00
            de forma diferida
            Fecha y hora 22 Ago 2024 04:46 PM
            Código de operación 1234567
            """;
        var r = VoucherConstanciaParser.Parse(ocr, 3800m);
        Assert.True(r.Ok);
        Assert.Contains(r.Advertencias, w => w.Contains("diferida", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Canal_esperado_incompatible_advierte()
    {
        const string ocr = """
            ¡Te Yapearon!
            S/ 20
            21 nov. 2026 08:46 p. m.
            Nro. de operación 20758411
            """;
        var r = VoucherConstanciaParser.Parse(ocr, 20m, canalEsperado: "BCP");
        Assert.True(r.Ok);
        Assert.Equal("Yape", r.CanalDetectado);
        Assert.Contains(r.Advertencias, w => w.Contains("BCP", StringComparison.OrdinalIgnoreCase));
    }
}
