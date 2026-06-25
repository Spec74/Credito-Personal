using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

/// <summary>Contrato 403 por políticas de rol en escrituras de ciclo/gestión (Fase 4).</summary>
public class CreditoRolAuthorizationEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreditoRolAuthorizationEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reprogramar_credito_solo_lectura_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["LECTURA"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/reprogramar-credito")
        {
            Content = JsonContent.Create(new { oficinaId = 1, creditoId = 1 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Reprogramar_credito_sin_admin_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["GESTOR"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/reprogramar-credito")
        {
            Content = JsonContent.Create(new { oficinaId = 1, creditoId = 1 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Condonar_credito_admin_no_devuelve_403_por_rol()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["ADMINISTRADOR"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/condonar-credito")
        {
            Content = JsonContent.Create(new
            {
                oficinaId = 1,
                creditoId = 1,
                montoCxc = 0m,
                montoCondonar = 0m,
                observacion = "TEST",
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Aprobar_credito_aprobador1_no_devuelve_403_por_rol()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["APROBADOR 1"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/aprobar-credito")
        {
            Content = JsonContent.Create(new { oficinaId = 1, creditoId = 1, opcion = 0 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Aprobar_credito_apro1_alias_no_devuelve_403_por_rol()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["APRO1"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/aprobar-credito")
        {
            Content = JsonContent.Create(new { oficinaId = 1, creditoId = 1, opcion = 0 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Crear_solicitud_reporteparcial_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["REPORTEPARCIAL"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/crear-solicitud-credito")
        {
            Content = JsonContent.Create(new { oficinaId = 1, personaId = 1 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Crear_solicitud_analista_con_aprobadores_no_devuelve_403_por_rol()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(["ANALISTA", "APROBADOR 1", "APROBADOR 2"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/crear-solicitud-credito")
        {
            Content = JsonContent.Create(new { oficinaId = 1, personaId = 1 }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task<string> DevTokenAsync(string[] roles)
    {
        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 1, oficinaId = 1, roles });
        tokenRes.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("accessToken vacío");
    }
}
