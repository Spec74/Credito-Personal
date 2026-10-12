using System.Text.Json;
using Credito.Modern.Infrastructure.Integraciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Integraciones;

internal static class WhatsAppWebhookEndpoints
{
    public const string Route = "/api/v1/integraciones/whatsapp/webhook";

    public static void MapWhatsAppWebhookEndpoints(this WebApplication app)
    {
        app.MapGet(Route, Verificar)
            .WithName("WhatsAppWebhookVerify")
            .WithTags("integraciones", "whatsapp")
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapPost(Route, Recibir)
            .WithName("WhatsAppWebhookReceive")
            .WithTags("integraciones", "whatsapp")
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    private static IResult Verificar(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        IOptions<WhatsAppOptions> options,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger("WhatsAppWebhook");
        var expected = options.Value.VerifyToken?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(expected))
        {
            log.LogWarning("WhatsApp webhook GET rechazado: VerifyToken no configurado.");
            return Results.Unauthorized();
        }

        if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(verifyToken?.Trim(), expected, StringComparison.Ordinal)
            || string.IsNullOrEmpty(challenge))
        {
            log.LogWarning(
                "WhatsApp webhook GET rechazado: mode={Mode} tokenMatch={Match}",
                mode,
                string.Equals(verifyToken?.Trim(), expected, StringComparison.Ordinal));
            return Results.Unauthorized();
        }

        log.LogInformation("WhatsApp webhook verificado por Meta.");
        return Results.Text(challenge, "text/plain");
    }

    private static async Task<IResult> Recibir(
        HttpRequest request,
        IOptions<WhatsAppOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var log = loggerFactory.CreateLogger("WhatsAppWebhook");
        var opts = options.Value;

        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var body = ms.ToArray();

        var appSecret = opts.AppSecret?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(appSecret))
        {
            var signature = request.Headers["X-Hub-Signature-256"].ToString();
            if (!WhatsAppWebhookSignature.EsValida(signature, body, appSecret))
            {
                log.LogWarning("WhatsApp webhook POST con firma inválida.");
                return Results.Unauthorized();
            }
        }
        else
        {
            log.LogWarning(
                "WhatsApp webhook POST sin AppSecret: se acepta el payload (configurar WhatsApp__AppSecret en producción).");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            RegistrarEventos(doc.RootElement, log);
        }
        catch (JsonException ex)
        {
            log.LogWarning(ex, "WhatsApp webhook POST con JSON inválido; se responde 200 para evitar reintentos inútiles.");
        }

        // Meta reintenta si no es 200; siempre ACK rápido.
        return Results.Ok();
    }

    private static void RegistrarEventos(JsonElement root, ILogger log)
    {
        if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                {
                    continue;
                }

                if (value.TryGetProperty("statuses", out var statuses) && statuses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var st in statuses.EnumerateArray())
                    {
                        var id = st.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                        var status = st.TryGetProperty("status", out var stEl) ? stEl.GetString() : null;
                        var recipient = st.TryGetProperty("recipient_id", out var rEl) ? rEl.GetString() : null;
                        log.LogInformation(
                            "WhatsApp status id={MessageId} status={Status} to=***{Last4}",
                            id,
                            status,
                            Last4(recipient));
                    }
                }

                if (value.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var msg in messages.EnumerateArray())
                    {
                        var from = msg.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : null;
                        var type = msg.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
                        log.LogInformation(
                            "WhatsApp inbound type={Type} from=***{Last4}",
                            type,
                            Last4(from));
                    }
                }
            }
        }
    }

    private static string Last4(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 4)
        {
            return "????";
        }

        return value[^4..];
    }
}
