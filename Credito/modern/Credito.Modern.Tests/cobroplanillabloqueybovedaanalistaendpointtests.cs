using System.Net;
using System.Net.Http.Json;

namespace Credito.Modern.Tests;

public class CobroPlanillaBloqueEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CobroPlanillaBloqueEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cobrar_planilla_bloque_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/cobrar-planilla-bloque",
            new
            {
                oficinaId = 1,
                cajaDiarioId = 1,
                planilla = new[]
                {
                    new { creditoId = 1, montoPagar = 10m, tipoPagoId = 1, fechaHoraTrans = (string?)null },
                },
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class TransferirBovedaAnalistaEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransferirBovedaAnalistaEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Transferir_boveda_analista_sin_jwt_devuelve_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credito/transferir-boveda-analista",
            new
            {
                oficinaId = 1,
                usuarioAnalistaId = 2,
                tipoPagoOrigenId = (short)2,
                importe = 10m,
                descripcion = "test",
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
