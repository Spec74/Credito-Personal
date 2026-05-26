using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Credito.Modern.Tests;

public class LoginEndpointTests : IClassFixture<CreditoModernWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginEndpointTests(CreditoModernWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_sin_datos_obligatorios_devuelve_400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { nombreUsuario = "", clave = "", oficinaId = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class LoginStrictAuthEndpointTests : IClassFixture<CreditoStrictAuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginStrictAuthEndpointTests(CreditoStrictAuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_sin_cliente_acceso_cuando_se_exige_devuelve_400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { nombreUsuario = "x", clave = "y", oficinaId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
