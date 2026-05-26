using System.Net;

namespace Credito.Modern.Tests;

public sealed class RptSimuladorPlanPagosEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptSimuladorPlanPagosEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_simulador_plan_pagos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync(
            "/api/v1/credito/rpt-simulador-plan-pagos?productoId=1&monto=1000&nroCuotas=12&interesMensual=5&fechaPrimerPago=2026-06-01&formaPago=M");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
