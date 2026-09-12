using Credito.Modern.Application.Prendario;
using Credito.Modern.Infrastructure.Integraciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Prendario;

public sealed class PrendarioAvisoEnvioService(
    IPrendarioReadService prendarioRead,
    WhatsAppCloudClient whatsApp,
    IOptions<WhatsAppOptions> options,
    IPrendarioWhatsAppPasadaStore pasadas,
    ILogger<PrendarioAvisoEnvioService> logger) : IPrendarioAvisoEnvioService
{
    public async Task<PrendarioAvisoEnvioResumenDto> EnviarPendientesAsync(
        int? oficinaId,
        int diasAntes,
        int? creditoId,
        string origen,
        CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        if (!cfg.TieneCredenciales)
        {
            var sinCredenciales = new PrendarioAvisoEnvioResumenDto(
                0,
                0,
                0,
                [],
                "WhatsApp Business no está configurado. Falta el token o el número de envío (user-secrets).");
            RegistrarPasada(origen, sinCredenciales);
            return sinCredenciales;
        }

        var pendientes = await prendarioRead
            .ListarAvisosVencimientoAsync(oficinaId, diasAntes, cancellationToken)
            .ConfigureAwait(false);
        if (creditoId is > 0)
        {
            pendientes = [.. pendientes.Where(x => x.CreditoId == creditoId.Value)];
        }

        var detalle = new List<PrendarioAvisoEnvioItemDto>(pendientes.Count);
        var enviados = 0;
        var fallidos = 0;
        var omitidos = 0;
        var delayMs = Math.Clamp(cfg.DelayBetweenMessagesMs, 0, 2000);

        for (var i = 0; i < pendientes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var aviso = pendientes[i];
            var (exito, mensaje) = await whatsApp
                .EnviarAvisoVencimientoAsync(
                    aviso.Celular ?? "",
                    aviso.NombreCliente,
                    aviso.FechaVencimiento,
                    aviso.MontoCancelar,
                    cancellationToken)
                .ConfigureAwait(false);

            if (exito)
            {
                await prendarioRead
                    .MarcarNotificadoWhatsAppAsync(aviso.OficinaId, aviso.CreditoId, cancellationToken)
                    .ConfigureAwait(false);
                enviados++;
            }
            else if (mensaje.Contains("celular", StringComparison.OrdinalIgnoreCase))
            {
                omitidos++;
            }
            else
            {
                fallidos++;
            }

            detalle.Add(new PrendarioAvisoEnvioItemDto(aviso.CreditoId, exito, mensaje));
            logger.LogInformation(
                "Aviso prendario crédito {CreditoId}: {Resultado}",
                aviso.CreditoId,
                exito ? "enviado" : mensaje);

            if (delayMs > 0 && i < pendientes.Count - 1)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        var resumen = new PrendarioAvisoEnvioResumenDto(enviados, fallidos, omitidos, detalle);
        RegistrarPasada(origen, resumen);
        return resumen;
    }

    private void RegistrarPasada(string origen, PrendarioAvisoEnvioResumenDto resumen)
    {
        var etiqueta = string.IsNullOrWhiteSpace(origen) ? "manual" : origen.Trim();
        var tz = WhatsAppZonaHoraria.Resolver(options.Value.TimeZoneId);
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        pasadas.Registrar(
            new PrendarioWhatsAppPasadaDto(
                ahora,
                etiqueta,
                resumen.Enviados,
                resumen.Fallidos,
                resumen.Omitidos,
                resumen.Advertencia));
    }
}
