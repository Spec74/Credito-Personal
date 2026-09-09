using Credito.Modern.Application.Prendario;
using Credito.Modern.Infrastructure.Integraciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Prendario;

public sealed class PrendarioAvisoEnvioService(
    IPrendarioReadService prendarioRead,
    WhatsAppCloudClient whatsApp,
    IOptions<WhatsAppOptions> options,
    ILogger<PrendarioAvisoEnvioService> logger) : IPrendarioAvisoEnvioService
{
    public async Task<PrendarioAvisoEnvioResumenDto> EnviarPendientesAsync(
        int? oficinaId,
        int diasAntes,
        int? creditoId = null,
        CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        if (!cfg.EstaConfigurado)
        {
            return new PrendarioAvisoEnvioResumenDto(
                0,
                0,
                0,
                [new PrendarioAvisoEnvioItemDto(0, false, "WhatsApp Business no está configurado.")]);
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

        foreach (var aviso in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        }

        return new PrendarioAvisoEnvioResumenDto(enviados, fallidos, omitidos, detalle);
    }
}
