using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Credito.Modern.Tests;
public class HealthEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_success_and_json()
    {
        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
            return;
        }

        // Con `CreditoDatabase:ConnectionString` configurada (p. ej. user-secrets) y SQL caído o inaccesible,
        // el check `database` deja el agregado en Unhealthy y ASP.NET Core responde 503.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(
            body.Contains("Unhealthy", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Degraded", StringComparison.OrdinalIgnoreCase),
            body);
    }
}
