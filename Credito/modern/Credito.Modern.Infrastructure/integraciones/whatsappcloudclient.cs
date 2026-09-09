using System.Net.Http.Headers;
using System.Text;
using Credito.Modern.Application.Prendario;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Integraciones;

public sealed class WhatsAppCloudClient(
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppCloudClient> logger)
{
    public const string HttpClientName = "WhatsAppCloud";
    private readonly WhatsAppOptions _opts = options.Value;

    public async Task<(bool Exito, string Mensaje)> EnviarAvisoVencimientoAsync(
        string celular,
        string nombreCliente,
        DateTime fechaVencimiento,
        decimal montoCancelar,
        CancellationToken cancellationToken)
    {
        if (!_opts.EstaConfigurado)
        {
            return (false, "WhatsApp Business no está configurado.");
        }

        var e164 = WhatsAppVencimientoPrendarioMensaje.NormalizarCelularPeru(celular);
        if (e164 is null)
        {
            return (false, "Número de celular vacío o inválido.");
        }

        var json = WhatsAppVencimientoPrendarioMensaje.CrearJson(
            e164,
            nombreCliente,
            fechaVencimiento,
            montoCancelar,
            _opts.TemplateVencimientoPrendario,
            _opts.TemplateLang);

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_opts.ApiVersion.Trim('/')}/{_opts.PhoneNumberId}/messages");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.Token);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var resp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                return (true, "Enviado");
            }

            logger.LogWarning(
                "WhatsApp Cloud API {Status} para crédito con destino {Destino}: {Cuerpo}",
                (int)resp.StatusCode,
                e164[^4..].PadLeft(e164.Length, '*'),
                TruncarCuerpo(body));
            return (false, MensajeUsuarioMeta(body, (int)resp.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "No se pudo contactar WhatsApp Cloud API");
            return (false, "No se pudo contactar WhatsApp Cloud API.");
        }
    }

    private static string TruncarCuerpo(string body) =>
        body.Length <= 400 ? body : body[..400];

    private static string MensajeUsuarioMeta(string body, int status)
    {
        if (body.Contains("template", StringComparison.OrdinalIgnoreCase)
            && (body.Contains("not exist", StringComparison.OrdinalIgnoreCase)
                || body.Contains("approved", StringComparison.OrdinalIgnoreCase)
                || body.Contains("paused", StringComparison.OrdinalIgnoreCase)))
        {
            return "La plantilla aviso_vencimiento_prendario aún no está aprobada o no coincide con el idioma configurado.";
        }

        if (body.Contains("recipient", StringComparison.OrdinalIgnoreCase)
            || body.Contains("(#131030)", StringComparison.Ordinal)
            || body.Contains("not a valid WhatsApp", StringComparison.OrdinalIgnoreCase))
        {
            return "El destino no está habilitado para este número de prueba de WhatsApp.";
        }

        return $"WhatsApp respondió HTTP {status}.";
    }
}
