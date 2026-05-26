using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Credito.Modern.Tests;

/// <summary>Host de prueba con <c>Menu:PermiteParametrosQuery=false</c> (sobrescribe el valor por defecto de <see cref="CreditoTestWebHost"/>).</summary>
public sealed class CreditoMenuQueryDisabledWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        CreditoTestWebHost.Configure(builder, requireClientAcceso: false);
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Menu:PermiteParametrosQuery"] = "false" });
            });
    }
}
