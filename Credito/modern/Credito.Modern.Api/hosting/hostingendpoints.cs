using Credito.Modern.Application.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Hosting;

internal static class HostingEndpoints
{
    public static void MapHostingEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/hosting/ui-config",
                (IOptions<HostingUiOptions> ui) =>
                {
                    var o = ui.Value;
                    var basePath = string.IsNullOrWhiteSpace(o.SpaBasePath) ? "/app" : o.SpaBasePath.TrimEnd('/');
                    return TypedResults.Ok(
                        new HostingUiConfigDto(
                            o.UseSpaForModule,
                            o.DefaultLoginToSpa,
                            basePath,
                            o.ObservacionDias < 1 ? 14 : o.ObservacionDias,
                            o.UseSpaForModule
                                ? "5C-7-cutover-spa-prioritario"
                                : "5C-7-piloto-dual"));
                })
            .WithName("HostingUiConfig")
            .WithTags("hosting")
            .AllowAnonymous()
            .Produces<HostingUiConfigDto>();
    }
}
