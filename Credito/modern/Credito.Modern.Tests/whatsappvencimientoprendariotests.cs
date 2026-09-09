using System.Text.Json;
using Credito.Modern.Application.Prendario;

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
}
