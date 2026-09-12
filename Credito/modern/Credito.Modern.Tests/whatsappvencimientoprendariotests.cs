using System.Text.Json;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Infrastructure.Integraciones;

namespace Credito.Modern.Tests;

public sealed class WhatsAppVencimientoPrendarioTests
{
    [Theory]
    [InlineData("982137430", "51982137430")]
    [InlineData("51 982 137 430", "51982137430")]
    [InlineData("123", null)]
    [InlineData(null, null)]
    public void Normaliza_celular_peruano(string? entrada, string? esperado)
    {
        Assert.Equal(esperado, WhatsAppVencimientoPrendarioMensaje.NormalizarCelularPeru(entrada));
    }

    [Fact]
    public void Plantilla_lleva_nombre_fecha_e_importe()
    {
        var json = WhatsAppVencimientoPrendarioMensaje.CrearJson(
            "51982137430",
            "JUAN PEREZ",
            new DateTime(2026, 10, 15),
            540.00m,
            "aviso_vencimiento_prendario",
            "es");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("whatsapp", root.GetProperty("messaging_product").GetString());
        Assert.Equal("51982137430", root.GetProperty("to").GetString());
        Assert.Equal("template", root.GetProperty("type").GetString());
        var template = root.GetProperty("template");
        Assert.Equal("aviso_vencimiento_prendario", template.GetProperty("name").GetString());
        Assert.Equal("es", template.GetProperty("language").GetProperty("code").GetString());
        var parametros = template.GetProperty("components")[0].GetProperty("parameters");
        Assert.Equal("JUAN PEREZ", parametros[0].GetProperty("text").GetString());
        Assert.Equal("15/10/2026", parametros[1].GetProperty("text").GetString());
        Assert.Equal("540.00", parametros[2].GetProperty("text").GetString());
    }

    [Fact]
    public void Proxima_corrida_hoy_si_aun_no_son_las_ocho()
    {
        var ahora = new DateTimeOffset(2026, 9, 10, 7, 30, 0, TimeSpan.FromHours(-5));
        var proxima = PrendarioWhatsAppProgramacion.ProximaCorrida(ahora, 8);
        Assert.Equal(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.FromHours(-5)), proxima);
    }

    [Fact]
    public void Proxima_corrida_manana_si_ya_pasaron_las_ocho()
    {
        var ahora = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.FromHours(-5));
        var proxima = PrendarioWhatsAppProgramacion.ProximaCorrida(ahora, 8);
        Assert.Equal(new DateTimeOffset(2026, 9, 11, 8, 0, 0, TimeSpan.FromHours(-5)), proxima);
    }

    [Fact]
    public void Kill_switch_no_borra_credenciales()
    {
        var opts = new WhatsAppOptions
        {
            Enabled = false,
            Token = "token",
            PhoneNumberId = "123",
        };
        Assert.True(opts.TieneCredenciales);
        Assert.False(opts.EstaConfigurado);
    }

    [Theory]
    [InlineData(401, "{\"error\":{\"message\":\"Authentication Error\",\"code\":190}}", true)]
    [InlineData(401, "Authentication Error", true)]
    [InlineData(400, "(#190) Invalid OAuth 2.0 Access Token", true)]
    [InlineData(400, "Template name does not exist in the translation", false)]
    [InlineData(400, "(#131030) Recipient is not a valid WhatsApp", false)]
    public void Mensaje_meta_distingue_token_plantilla_y_destino(int status, string body, bool esToken)
    {
        var msg = WhatsAppCloudClient.MensajeParaUsuario(body, status);
        if (esToken)
        {
            Assert.Contains("token", msg, StringComparison.OrdinalIgnoreCase);
            return;
        }

        Assert.DoesNotContain("user-secrets", msg, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            msg.Contains("plantilla", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("destino", StringComparison.OrdinalIgnoreCase));
    }
}
