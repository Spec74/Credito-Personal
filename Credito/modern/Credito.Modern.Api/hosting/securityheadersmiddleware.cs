namespace Credito.Modern.Api.Hosting;

/// <summary>
/// Cabeceras de endurecimiento del navegador (API JSON + Swagger en Development).
/// CSP permisiva en Development para Swagger UI; en el resto de ambientes más estricta.
/// </summary>
internal static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseCreditoSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-site";

            var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                // Swagger UI carga scripts/estilos inline y CDN locales del paquete.
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'";
            }
            else
            {
                // API JSON: sin scripts. La SPA vive en otro origen (Vercel) con su propio CSP.
                headers["Content-Security-Policy"] =
                    "default-src 'none'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'none'; " +
                    "form-action 'none'";
            }

            await next().ConfigureAwait(false);
        });
    }
}
