using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Credito.Modern.Tests;

/// <summary>Garantiza Jwt:SigningKey válida en tests (CI Release / cualquier entorno).</summary>
public sealed class CreditoModernWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        CreditoTestWebHost.Configure(builder, requireClientAcceso: false);
}
