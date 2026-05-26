using System.Net;

namespace Credito.Modern.Tests;

public sealed class MovimientoCajaTicketEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MovimientoCajaTicketEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ticket_pdf_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/movimiento-caja-ticket-pdf?oficinaId=1&movimientoCajaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
