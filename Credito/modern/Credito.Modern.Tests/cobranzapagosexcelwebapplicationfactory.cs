using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Credito.Modern.Tests;

public sealed class CobranzaPagosExcelWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        CreditoTestWebHost.Configure(builder, requireClientAcceso: false);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IRptCobroDiarioDetalleReadService>();
            services.RemoveAll<IUsuarioAdminReadService>();
            services.RemoveAll<IOficinaReadService>();
            services.AddScoped<IRptCobroDiarioDetalleReadService, FakeRptCobroDiarioDetalleReadService>();
            services.AddScoped<IUsuarioAdminReadService, FakeUsuarioAdminReadService>();
            services.AddScoped<IOficinaReadService, FakeOficinaReadService>();
        });
    }
}
