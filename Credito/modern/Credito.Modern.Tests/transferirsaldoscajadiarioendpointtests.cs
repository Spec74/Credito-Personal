using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Credito.Modern.Tests;

public class TransferirSaldosCajaDiarioEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransferirSaldosCajaDiarioEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Transferir_saldos_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/transferir-saldos-caja-diario",
            new
            {
                oficinaId = 1,
                cajaDiarioId = 1,
                importe = 1m,
                descripcion = "test",
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirmar_clave_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/confirmar-clave-caja-diario",
            new { clave = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Transferir_saldos_oficina_distinta_al_token_devuelve_403()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = await DevTokenAsync(1, 1);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/credito/transferir-saldos-caja-diario")
        {
            Content = JsonContent.Create(new
            {
                oficinaId = 999,
                cajaDiarioId = 1,
                importe = 1m,
                descripcion = "test",
            }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private async Task<string> DevTokenAsync(int usuarioId, int oficinaId)
    {
        var tokenRes = await _client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new { usuarioId, oficinaId });
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }
}
