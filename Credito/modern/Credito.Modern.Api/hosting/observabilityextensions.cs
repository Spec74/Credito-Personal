using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Credito.Modern.Api.Hosting;

internal static class ObservabilityExtensions
{
    public const string ServiceName = "Credito.Modern.Api";

    public static WebApplicationBuilder AddCreditoObservability(this WebApplicationBuilder builder)
    {
        builder.Logging.Configure(options =>
        {
            options.ActivityTrackingOptions =
                ActivityTrackingOptions.SpanId
                | ActivityTrackingOptions.TraceId
                | ActivityTrackingOptions.ParentId;
        });

        var otlp = builder.Configuration["OpenTelemetry:OtlpEndpoint"]?.Trim();
        var enableConsole = builder.Configuration.GetValue("OpenTelemetry:ConsoleExporter", builder.Environment.IsDevelopment());

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation(o =>
                {
                    o.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health")
                        && !ctx.Request.Path.StartsWithSegments("/health/ready");
                });
                t.AddHttpClientInstrumentation();
                if (enableConsole)
                    t.AddConsoleExporter();
                if (!string.IsNullOrWhiteSpace(otlp))
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation();
                m.AddRuntimeInstrumentation();
                if (enableConsole)
                    m.AddConsoleExporter();
                if (!string.IsNullOrWhiteSpace(otlp))
                    m.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
            });

        return builder;
    }

    /// <summary>Enriquece el scope de log con CorrelationId en cada request.</summary>
    public static IApplicationBuilder UseCreditoCorrelationLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Items["CorrelationId"] as string
                ?? context.Response.Headers["X-Correlation-ID"].ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                await next().ConfigureAwait(false);
                return;
            }

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Credito.Correlation");

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
            }))
            {
                Activity.Current?.SetTag("correlation.id", correlationId);
                await next().ConfigureAwait(false);
            }
        });
    }
}
