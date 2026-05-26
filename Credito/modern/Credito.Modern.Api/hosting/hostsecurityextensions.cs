using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Hosting;

internal static class HostSecurityExtensions
{
    internal static WebApplicationBuilder AddCreditoHostSecurity(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<HostingPipelineOptions>()
            .BindConfiguration(HostingPipelineOptions.SectionName);
        builder.Services.AddOptions<HostingUiOptions>().BindConfiguration(HostingUiOptions.SectionName);
        builder.Services.AddOptions<HostingUiOptions>().BindConfiguration(HostingUiOptions.SectionName);

        builder.Services
            .AddOptions<MenuNavigationOptions>()
            .BindConfiguration(MenuNavigationOptions.SectionName);

        builder.Services
            .AddOptions<ForwardedHeadersBindingOptions>()
            .BindConfiguration(ForwardedHeadersBindingOptions.SectionName)
            .Validate(
                o =>
                {
                    if (!o.Enabled)
                        return true;
                    if (o.KnownProxies is not { Length: > 0 })
                        return false;
                    foreach (var s in o.KnownProxies)
                    {
                        if (string.IsNullOrWhiteSpace(s))
                            continue;
                        if (IPAddress.TryParse(s.Trim(), out _))
                            return true;
                    }

                    return false;
                },
                "Hosting:ForwardedHeaders: con Enabled=true, KnownProxies debe incluir al menos una IP válida.")
            .ValidateOnStart();

        builder.Services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ConfigureForwardedHeadersOptions>();

        builder.Services
            .AddOptions<BrowserCorsOptions>()
            .BindConfiguration(BrowserCorsOptions.SectionName);

        builder.Services
            .AddOptions<LoginRateLimitOptions>()
            .BindConfiguration(LoginRateLimitOptions.SectionName)
            .Validate(
                o =>
                    o.Disabled
                    || (o.LoginPermitLimit >= 1
                        && o.LoginWindowSeconds >= 1
                        && o.RefreshPermitLimit >= 1
                        && o.RefreshWindowSeconds >= 1),
                "RateLimiting: con Disabled=false, LoginPermitLimit, LoginWindowSeconds, RefreshPermitLimit y RefreshWindowSeconds deben ser >= 1.")
            .ValidateOnStart();

        var corsSnapshot = builder.Configuration.GetSection(BrowserCorsOptions.SectionName).Get<BrowserCorsOptions>() ?? new();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "browser",
                policy =>
                {
                    if (builder.Environment.IsDevelopment() && corsSnapshot.AllowedOrigins.Length == 0)
                    {
                        policy.SetIsOriginAllowed(_ => true);
                    }
                    else if (corsSnapshot.AllowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(corsSnapshot.AllowedOrigins);
                    }
                    else
                    {
                        policy.SetIsOriginAllowed(_ => false);
                    }

                    policy.AllowAnyHeader().AllowAnyMethod();
                });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                var isRefresh = context.HttpContext.Request.Path.StartsWithSegments("/api/v1/auth/refresh");
                var detail = isRefresh
                    ? "Has superado el límite de solicitudes de refresh por IP. Espera unos minutos o contacta al administrador."
                    : "Has superado el límite de intentos de login por IP. Espera unos minutos o contacta al administrador.";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        title = "Demasiadas solicitudes",
                        status = StatusCodes.Status429TooManyRequests,
                        detail,
                    },
                    cancellationToken: ct);
            };

            options.AddPolicy(
                "auth-login",
                context =>
                {
                    var opts = context.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;
                    if (opts.Disabled)
                    {
                        return RateLimitPartition.GetNoLimiter("disabled");
                    }

                    var window = TimeSpan.FromSeconds(opts.LoginWindowSeconds);
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = opts.LoginPermitLimit,
                            Window = window,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        });
                });

            options.AddPolicy(
                "auth-refresh",
                context =>
                {
                    var opts = context.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;
                    if (opts.Disabled)
                    {
                        return RateLimitPartition.GetNoLimiter("disabled");
                    }

                    var window = TimeSpan.FromSeconds(opts.RefreshWindowSeconds);
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        "refresh-" + ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = opts.RefreshPermitLimit,
                            Window = window,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        });
                });
        });

        return builder;
    }

    internal static void UseCreditoHostSecurity(this WebApplication app)
    {
        var fwd = app.Configuration.GetSection(ForwardedHeadersBindingOptions.SectionName).Get<ForwardedHeadersBindingOptions>() ?? new();
        if (fwd.Enabled)
        {
            app.UseForwardedHeaders();
        }

        var hosting = app.Configuration.GetSection(HostingPipelineOptions.SectionName).Get<HostingPipelineOptions>() ?? new();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        if (!hosting.DisableHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("browser");
        app.UseRateLimiter();
    }
}
