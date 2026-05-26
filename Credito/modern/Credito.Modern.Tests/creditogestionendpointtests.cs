using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CreditoGestionEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreditoGestionEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Credito_contexto_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/credito-contexto?creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Persona_credito_ficha_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/persona-credito-ficha?oficinaId=1&personaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Cargos_credito_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/cargos-credito?oficinaId=1&creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Condonar_credito_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/condonar-credito",
            new { oficinaId = 1, creditoId = 1, montoCxc = 1m, montoCondonacion = 1m, observacion = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Observar_credito_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/observar-credito",
            new { oficinaId = 1, creditoId = 1, observacion = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guardar_cargo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/guardar-cargo-credito",
            new
            {
                oficinaId = 1,
                creditoId = 1,
                tipoCargoId = 1,
                monto = 10m,
                descripcion = "test",
                final = false,
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Creditos_grilla_persona_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/creditos-grilla-persona?oficinaId=1&personaId=1&grupoActivo=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Actualizar_irrecuperable_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/actualizar-irrecuperable-credito",
            new { oficinaId = 1, creditoId = 1, indIrrecuperable = true });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Modificar_tramite_adm_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/modificar-tramite-adm-credito",
            new { oficinaId = 1, creditoId = 1, valor = 10m });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Modificar_central_riesgo_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/modificar-central-riesgo-credito",
            new { oficinaId = 1, creditoId = 1, valor = 5m });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Actualizar_descuento_plan_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/actualizar-descuento-plan-pago",
            new { oficinaId = 1, creditoId = 1, planPagoId = 1, descuento = 1m });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Actualizar_aval_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/actualizar-aval-credito",
            new { oficinaId = 1, creditoId = 1, personaAvalId = (int?)null });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
