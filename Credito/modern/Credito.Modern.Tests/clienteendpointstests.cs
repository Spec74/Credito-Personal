using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class ClienteEndpointsTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClienteEndpointsTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Clientes_listar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/clientes/listar?page=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clientes_por_documento_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/clientes/por-documento?documento=12345678");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Clientes_buscar_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/clientes/buscar?term=abc");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clientes_detalle_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/clientes/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clientes_existe_documento_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/clientes/existe-documento?documento=12345678");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clientes_guardar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/clientes/guardar",
            new
            {
                clienteId = 0,
                tipoPersona = "N",
                nombre = "JUAN",
                apePaterno = "PEREZ",
                apeMaterno = "LOPEZ",
                numeroDocumento = "12345678",
                sexoMasculino = true,
                calificacion = "A",
                activo = true,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clientes_activar_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/clientes/1/activar", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clientes_bloquear_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsync("/api/v1/clientes/1/bloquear", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiPeru_dni_sin_token_no_filtra_configuracion_interna()
    {
        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId = 1, oficinaId = 1, roles = new[] { "ANALISTA" } });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);

        using var tokenDoc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = tokenDoc.RootElement.GetProperty("accessToken").GetString();
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/integraciones/apiperu/dni/12345678");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":false", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiPeru:Token", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user-secrets", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("variables de entorno", content, StringComparison.OrdinalIgnoreCase);
    }
}
