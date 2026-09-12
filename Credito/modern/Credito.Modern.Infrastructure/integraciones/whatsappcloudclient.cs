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

    public async Task<(bool Exito, string Mensaje)> EnviarAvisoVencimientoAsync(
        string celular,
        string nombreCliente,
        DateTime fechaVencimiento,
        decimal montoCancelar,
        CancellationToken cancellationToken)
    {
        var opts = options.Value;
        if (!opts.TieneCredenciales)
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
            opts.TemplateVencimientoPrendario,
            opts.TemplateLang);

        var version = string.IsNullOrWhiteSpace(opts.ApiVersion) ? "v25.0" : opts.ApiVersion.Trim('/');
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"{version}/{opts.PhoneNumberId.Trim()}/messages");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.Token.Trim());
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
            return (false, MensajeParaUsuario(body, (int)resp.StatusCode));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "No se pudo contactar WhatsApp Cloud API");
            return (false, "No se pudo contactar WhatsApp Cloud API.");
        }
    }

    private static string TruncarCuerpo(string body) =>
        body.Length <= 400 ? body : body[..400];

    public static string MensajeParaUsuario(string body, int status)
    {
        var texto = body ?? string.Empty;
        if (status == 401
            || texto.Contains("(#190)", StringComparison.Ordinal)
            || texto.Contains("\"code\":190", StringComparison.Ordinal)
            || texto.Contains("Authentication Error", StringComparison.OrdinalIgnoreCase)
            || texto.Contains("Invalid OAuth", StringComparison.OrdinalIgnoreCase)
            || (texto.Contains("access token", StringComparison.OrdinalIgnoreCase)
                && texto.Contains("expired", StringComparison.OrdinalIgnoreCase)))
        {
            return "El token de WhatsApp no es válido o expiró. En local: user-secrets WhatsApp:Token. En servidores: variable WhatsApp__Token.";
        }

        if (texto.Contains("template", StringComparison.OrdinalIgnoreCase)
            && (texto.Contains("not exist", StringComparison.OrdinalIgnoreCase)
                || texto.Contains("approved", StringComparison.OrdinalIgnoreCase)
                || texto.Contains("paused", StringComparison.OrdinalIgnoreCase)
                || texto.Contains("(#132001)", StringComparison.Ordinal)
                || texto.Contains("(#132015)", StringComparison.Ordinal)))
        {
            return "La plantilla aviso_vencimiento_prendario aún no está aprobada o no coincide con el idioma configurado.";
        }

        if (texto.Contains("recipient", StringComparison.OrdinalIgnoreCase)
            || texto.Contains("(#131030)", StringComparison.Ordinal)
            || texto.Contains("not a valid WhatsApp", StringComparison.OrdinalIgnoreCase))
        {
            return "El destino no está habilitado para este número de prueba de WhatsApp.";
        }

        return $"WhatsApp respondió HTTP {status}.";
    }
}
