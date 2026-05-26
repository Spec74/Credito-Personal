using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public sealed class ReportesExportPoliticaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportesExportPoliticaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reportes_politica_exportacion_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/reportes/politica-exportacion");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reportes_politica_exportacion_con_jwt_devuelve_politica_estable()
    {
        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        tokenRes.EnsureSuccessStatusCode();
        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var token = tokenDoc.RootElement.GetProperty("accessToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reportes/politica-exportacion");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.GetProperty("csvTabularCompleto").GetBoolean());
        Assert.Equal(
            "6A-ui-sin-legacy-por-defecto; csv-tabular-completo; pdf-tabular-completo",
            root.GetProperty("fase").GetString());
        Assert.Equal("/api/v1/reportes/catalogo", root.GetProperty("catalogoModerno").GetString());

        var textoItems = root.GetProperty("informesTextoRdlcSinCsv");
        Assert.Equal(3, textoItems.GetArrayLength());

        var pilotos = root.GetProperty("informesPdfPilotos");
        Assert.Equal(40, pilotos.GetArrayLength());

        var motor = root.GetProperty("rdlcMotor");
        Assert.Equal("layout-rdlc-fase4-opcional", motor.GetProperty("estado").GetString());
        Assert.Contains("ReporteController", motor.GetProperty("legacyController").GetString());
    }
}
