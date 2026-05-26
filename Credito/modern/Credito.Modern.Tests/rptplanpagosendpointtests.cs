using System.Net;

namespace Credito.Modern.Tests;

public sealed class RptPlanPagosEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RptPlanPagosEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_plan_pagos_sin_jwt_devuelve_401()
    {
        var response = await _client.GetAsync("/api/v1/credito/rpt-plan-pagos?creditoId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
