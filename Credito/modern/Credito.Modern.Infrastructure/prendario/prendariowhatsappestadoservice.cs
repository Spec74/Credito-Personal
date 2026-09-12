using Credito.Modern.Application.Prendario;
using Credito.Modern.Infrastructure.Integraciones;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Prendario;

public sealed class PrendarioWhatsAppEstadoService(
    IOptions<WhatsAppOptions> options,
    IPrendarioWhatsAppPasadaStore pasadas) : IPrendarioWhatsAppEstadoService
{
    public PrendarioWhatsAppEstadoDto Obtener()
    {
        var cfg = options.Value;
        var tz = WhatsAppZonaHoraria.Resolver(cfg.TimeZoneId);
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var hora = Math.Clamp(cfg.DailyHourLocal, 0, 23);
        var proxima = PrendarioWhatsAppProgramacion.ProximaCorrida(ahora, hora);
        return new PrendarioWhatsAppEstadoDto(
            cfg.Enabled,
            cfg.TieneCredenciales,
            cfg.EstaConfigurado,
            Math.Clamp(cfg.DiasAntes, 1, 30),
            hora,
            proxima,
            cfg.RunOnStartupIfPending,
            pasadas.Ultima);
    }
}
