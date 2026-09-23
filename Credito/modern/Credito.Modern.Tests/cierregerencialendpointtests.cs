using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Tests;

public class CierreGerencialEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CreditoModernWebApplicationFactory _factory;

    public CierreGerencialEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Permisos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/cierre-gerencial/permisos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Avance_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/cierre-gerencial/avance?periodo=2026-09-01");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Metas_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/cierre-gerencial/metas?periodo=2026-09-01");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Excel_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/cierre-gerencial/excel?periodo=2026-09-01");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_metas_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/cierre-gerencial/metas",
            new
            {
                periodo = "2026-09-01",
                metas = new[]
                {
                    new
                    {
                        usuarioId = 1,
                        tipoCartera = "PRODUCTIVA",
                        metaCapitalCierre = 1000m,
                        metaClientesActivosCierre = 10,
                        metaVencidosMaximoCierre = 100m,
                        metaRecuperacionVencidosMes = 50m,
                    },
                },
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Avance_usuario_sin_acl_devuelve_403()
    {
        if (Ci()) return;

        var token = await DevTokenAsync(usuarioId: 99991, roles: ["ANALISTA"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/cierre-gerencial/avance?periodo=2026-09-01");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Avance_admin_fuera_de_lista_devuelve_403()
    {
        if (Ci()) return;

        // Paridad MVC: ADMINISTRADOR no basta; debe estar en UsuarioConsultaIds.
        var token = await DevTokenAsync(usuarioId: 88881, roles: ["ADMINISTRADOR"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/cierre-gerencial/avance?periodo=2026-09-01");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Permisos_usuario_consulta_configurado_puede_consultar()
    {
        if (Ci()) return;

        var consultaIds = _factory.Services.GetRequiredService<IOptions<CierreGerencialOptions>>()
            .Value.UsuarioConsultaIds;
        Assert.NotNull(consultaIds);
        Assert.NotEmpty(consultaIds!);

        var token = await DevTokenAsync(usuarioId: consultaIds![0], roles: ["ANALISTA"]);
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cierre-gerencial/permisos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        Assert.True(doc.RootElement.GetProperty("puedeConsultar").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("puedeGestionarMetas").GetBoolean());
    }

    [Fact]
    public async Task Excel_con_acl_devuelve_xlsx_o_conflicto_sql()
    {
        if (Ci()) return;

        var consultaIds = _factory.Services.GetRequiredService<IOptions<CierreGerencialOptions>>()
            .Value.UsuarioConsultaIds;
        var token = await DevTokenAsync(usuarioId: consultaIds![0], roles: ["ANALISTA"]);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/cierre-gerencial/excel?periodo=2026-09-01");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);

        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
        if (res.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                res.Content.Headers.ContentType?.MediaType);
        }
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
