using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Credito.Modern.Tests;

/// <summary>
/// Configuración compartida para <see cref="WebApplicationFactory{TEntryPoint}"/>:
/// claves JWT de prueba y logging acotado al ruido típico de integración.
/// </summary>
/// <remarks>
/// <para>
/// En <c>dotnet test</c> el host in-process repite muchas veces mensajes que en desarrollo son útiles pero aquí solo ensucian la salida:
/// </para>
/// <list type="bullet">
/// <item><description><c>Microsoft.Extensions.Hosting.Internal.Host</c> en Debug (varios hosts en paralelo).</description></item>
/// <item><description><c>Microsoft.AspNetCore.DataProtection</c> al elegir repositorio de claves por cada instancia.</description></item>
/// <item><description><c>Microsoft.AspNetCore.Hosting.Diagnostics</c> y enrutamiento: cada request/response y “Executing endpoint”.</description></item>
/// <item><description><c>JwtBearerHandler</c> en Information cuando un test envía a propósito un Bearer no-JWT (caso negativo); no indica fallo del pipeline.</description></item>
/// </list>
/// <para>
/// <c>SetMinimumLevel(Warning)</c> solo no bastaba: el Api carga <c>appsettings.Development.json</c> con <c>Default=Debug</c> y el host de prueba usa Development,
/// así que la configuración de archivos volvía a habilitar Information/Debug. Se añaden claves <c>Logging:LogLevel:*</c> en la colección en memoria para
/// imponer Warning en categorías ruidosas de Microsoft. El prefijo <c>Credito.Modern</c> queda en <see cref="LogLevel.Information"/> por si hace falta
/// depurar la API en una prueba concreta.
/// </para>
/// </remarks>
internal static class CreditoTestWebHost
{
    public static void Configure(IWebHostBuilder builder, bool requireClientAcceso)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "TestJwtSigningKey___must_be_32_bytes__",
                    ["Jwt:Issuer"] = "Credito.Modern.Tests",
                    ["Jwt:Audience"] = "Credito.Modern.Tests",
                    ["Jwt:RefreshAudience"] = "Credito.Modern.Tests.Refresh",
                    ["Jwt:AccessTokenLifetimeHours"] = "8",
                    ["Jwt:RefreshTokenLifetimeDays"] = "7",
                    ["Jwt:RefreshTokenVersion"] = "1",
                    ["Auth:RequerirClienteAcceso"] = requireClientAcceso ? "true" : "false",
                    ["Auth:MigracionClavePerezosa"] = "false",
                    ["Hosting:DisableHttpsRedirection"] = "true",
                    ["Hosting:AllowDevToken"] = "true",
                    ["Menu:PermiteParametrosQuery"] = "true",
                    ["RateLimiting:Disabled"] = "true",
                    ["WhatsApp:Enabled"] = "false",
                    ["WhatsApp:RunOnStartupIfPending"] = "false",
                    ["WhatsApp:Token"] = "",
                    // El Api trae appsettings.Development.json (Default=Debug) y el host de prueba usa Development:
                    // sin esto, el ruido de Hosting, DataProtection, cada request y JwtBearer (caso negativo) vuelve a salir.
                    ["Logging:LogLevel:Default"] = "Warning",
                    ["Logging:LogLevel:Microsoft"] = "Warning",
                    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore.Authentication"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore.DataProtection"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore.Hosting.Diagnostics"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore.Routing"] = "Warning",
                    ["Logging:LogLevel:Microsoft.Extensions.Hosting"] = "Warning",
                    ["Logging:LogLevel:Microsoft.IdentityModel"] = "Warning",
                });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("Credito.Modern", LogLevel.Information);
        });
    }
}
