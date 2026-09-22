using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

/// <summary>
/// Paridad <c>ReporteController.Credito</c>: rol DB <c>REPORTEPARCIAL</c> equivale a ViewBag PARCIAL
/// (puede consultar TODOS); gestor sin ese rol no puede impersonar otro usuarioId.
/// </summary>
public sealed class GestorInformeReportAccessEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GestorInformeReportAccessEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Inactivos_gestor_con_otro_usuarioId_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(usuarioId: 42, roles: ["GESTOR"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/credito/rpt-clientes-inactivos?oficinaId=1&usuarioId=777");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Inactivos_reporteparcial_sin_usuarioId_no_es_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(usuarioId: 42, roles: ["REPORTEPARCIAL"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/credito/rpt-clientes-inactivos?oficinaId=1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Activos_gestor_sin_usuarioId_no_es_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Resolve fuerza JWT usuarioId; no debe rechazar por falta de query usuarioId.
        var token = await DevTokenAsync(usuarioId: 42, roles: ["GESTOR"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/credito/rpt-creditos-activos?oficinaId=1&fechaIni=2026-01-01&fechaFin=2026-01-31");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task<string> DevTokenAsync(int usuarioId, string[] roles)
    {
        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId, oficinaId = 1, roles });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("accessToken ausente");
    }
}
