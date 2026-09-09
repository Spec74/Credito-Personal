using Credito.Modern.Application.Prendario;
using Credito.Modern.Infrastructure.Integraciones;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Prendario;

/// <summary>
/// Envío diario de la plantilla de vencimiento prendario (3 días antes).
/// Sustituye el temporizador de <c>Global.asax</c> del legado.
/// </summary>
public sealed class PrendarioVencimientoWhatsAppHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<WhatsAppOptions> options,
    ILogger<PrendarioVencimientoWhatsAppHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = options.Value;
        if (!cfg.Enabled)
        {
            logger.LogInformation("Aviso WhatsApp prendario desactivado (WhatsApp:Enabled=false).");
            return;
        }

        if (!cfg.EstaConfigurado)
        {
            logger.LogWarning(
                "WhatsApp prendario está Enabled pero faltan WhatsApp:Token o WhatsApp:PhoneNumberId (user-secrets). No se enviará nada.");
        }

        if (cfg.RunOnStartupIfPending)
        {
            await EjecutarPasadaAsync("arranque", stoppingToken).ConfigureAwait(false);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = TiempoHastaProximaCorrida(cfg);
            logger.LogInformation(
                "Próximo aviso WhatsApp prendario en {Horas:0.0} h (hora local {Hora}:00).",
                espera.TotalHours,
                cfg.DailyHourLocal);
            try
            {
                await Task.Delay(espera, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await EjecutarPasadaAsync("programada", stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task EjecutarPasadaAsync(string origen, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var envio = scope.ServiceProvider.GetRequiredService<IPrendarioAvisoEnvioService>();
            var dias = Math.Clamp(options.Value.DiasAntes, 1, 30);
            var resumen = await envio
                .EnviarPendientesAsync(oficinaId: null, dias, creditoId: null, stoppingToken)
                .ConfigureAwait(false);
            logger.LogInformation(
                "Pasada WhatsApp prendario ({Origen}): enviados {Enviados}, fallidos {Fallidos}, omitidos {Omitidos}.",
                origen,
                resumen.Enviados,
                resumen.Fallidos,
                resumen.Omitidos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falló la pasada WhatsApp prendario ({Origen}).", origen);
        }
    }

    internal static TimeSpan TiempoHastaProximaCorrida(WhatsAppOptions cfg)
    {
        var tz = ResolverZona(cfg.TimeZoneId);
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var hora = Math.Clamp(cfg.DailyHourLocal, 0, 23);
        var proxima = new DateTimeOffset(
            ahora.Year, ahora.Month, ahora.Day, hora, 0, 0, ahora.Offset);
        if (proxima <= ahora)
        {
            proxima = proxima.AddDays(1);
        }

        return proxima - ahora;
    }

    private static TimeZoneInfo ResolverZona(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            var fallback = OperatingSystem.IsWindows()
                ? "SA Pacific Standard Time"
                : "America/Lima";
            return TimeZoneInfo.FindSystemTimeZoneById(fallback);
        }
    }
}
