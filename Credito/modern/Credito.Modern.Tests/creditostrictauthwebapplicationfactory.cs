using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Credito.Modern.Tests;

/// <summary>Igual que la factory principal de tests pero con <c>Auth:RequerirClienteAcceso=true</c>.</summary>
public sealed class CreditoStrictAuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        CreditoTestWebHost.Configure(builder, requireClientAcceso: true);
}
