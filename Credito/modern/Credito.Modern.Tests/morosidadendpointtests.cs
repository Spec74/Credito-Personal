using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Tests;

public class MorosidadEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CreditoModernWebApplicationFactory _factory;

    public MorosidadEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Permisos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/morosidad/permisos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Empresa_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/morosidad/empresa");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Excel_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/morosidad/excel?fechaInicio=2026-09-01&fechaFin=2026-09-30");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Empresa_usuario_sin_acl_devuelve_403()
    {
        if (Ci()) return;

        var token = await DevTokenAsync(usuarioId: 99991, roles: ["ANALISTA"]);
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/morosidad/empresa");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Permisos_usuario_consulta_configurado_puede_consultar()
    {
        if (Ci()) return;

        var consultaIds = _factory.Services.GetRequiredService<IOptions<MorosidadOptions>>()
            .Value.UsuarioConsultaIds;
        Assert.NotNull(consultaIds);
        Assert.NotEmpty(consultaIds!);

        var token = await DevTokenAsync(usuarioId: consultaIds![0], roles: ["ANALISTA"]);
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/morosidad/permisos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("puedeConsultar").GetBoolean());
    }

    private async Task<string> DevTokenAsync(int usuarioId, string[] roles)
    {
        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId, oficinaId = 1, roles });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static bool Ci() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
}
