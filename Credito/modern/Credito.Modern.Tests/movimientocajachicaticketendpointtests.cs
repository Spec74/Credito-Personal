using System.Net;

namespace Credito.Modern.Tests;

public sealed class MovimientoCajaChicaTicketEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MovimientoCajaChicaTicketEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ticket_caja_chica_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/movimiento-caja-chica-ticket-pdf?movimientoCajaChicaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
