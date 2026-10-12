using System.Net;
using System.Security.Cryptography;
using System.Text;
using Credito.Modern.Infrastructure.Integraciones;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Credito.Modern.Tests;

public sealed class WhatsAppWebhookSignatureTests
{
    [Fact]
    public void Firma_hmac_sha256_valida()
    {
        var secret = "test-app-secret";
        var body = Encoding.UTF8.GetBytes("""{"object":"whatsapp_business_account"}""");
        var hex = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();
        Assert.True(WhatsAppWebhookSignature.EsValida($"sha256={hex}", body, secret));
    }

    [Fact]
    public void Firma_invalida_si_cambia_body()
    {
        var secret = "test-app-secret";
        var body = Encoding.UTF8.GetBytes("""{"object":"whatsapp_business_account"}""");
        var hex = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();
        Assert.False(WhatsAppWebhookSignature.EsValida($"sha256={hex}", "otro"u8, secret));
    }
}

public sealed class WhatsAppWebhookEndpointTests : IClassFixture<WhatsAppWebhookWebApplicationFactory>
{
    private const string Route = "/api/v1/integraciones/whatsapp/webhook";
    private readonly HttpClient _client;

    public WhatsAppWebhookEndpointTests(WhatsAppWebhookWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Get_challenge_ok_con_verify_token()
    {
        var url =
            $"{Route}?hub.mode=subscribe&hub.verify_token=credix-test-verify&hub.challenge=12345";
        using var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("12345", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_challenge_401_con_token_incorrecto()
    {
        var url = $"{Route}?hub.mode=subscribe&hub.verify_token=malo&hub.challenge=12345";
        using var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Get_challenge_401_sin_challenge()
    {
        var url = $"{Route}?hub.mode=subscribe&hub.verify_token=credix-test-verify";
        using var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Post_status_con_firma_valida_responde_200()
    {
        var payload =
            """{"object":"whatsapp_business_account","entry":[{"changes":[{"value":{"statuses":[{"id":"wamid.1","status":"sent","recipient_id":"51982137430"}]},"field":"messages"}]}]}""";
        var body = Encoding.UTF8.GetBytes(payload);
        var hex = Convert.ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes("test-app-secret"), body))
            .ToLowerInvariant();

        using var req = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = new ByteArrayContent(body),
        };
        req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        req.Headers.TryAddWithoutValidation("X-Hub-Signature-256", $"sha256={hex}");

        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Post_firma_invalida_responde_401()
    {
        var payload = """{"object":"whatsapp_business_account"}""";
        using var req = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req.Headers.TryAddWithoutValidation("X-Hub-Signature-256", "sha256=deadbeef");

        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}

public sealed class WhatsAppWebhookProdFailClosedTests : IClassFixture<WhatsAppWebhookProdFactory>
{
    private const string Route = "/api/v1/integraciones/whatsapp/webhook";
    private readonly HttpClient _client;

    public WhatsAppWebhookProdFailClosedTests(WhatsAppWebhookProdFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Post_sin_app_secret_en_production_responde_401()
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = new StringContent("""{"object":"whatsapp_business_account"}""", Encoding.UTF8, "application/json"),
        };
        using var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}

public sealed class WhatsAppWebhookWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        CreditoTestWebHost.Configure(builder, requireClientAcceso: false);
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["WhatsApp:VerifyToken"] = "credix-test-verify",
                        ["WhatsApp:AppSecret"] = "test-app-secret",
                    });
            });
    }
}

/// <summary>Simula Production sin AppSecret (fail-closed).</summary>
public sealed class WhatsAppWebhookProdFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        CreditoTestWebHost.Configure(builder, requireClientAcceso: false);
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["WhatsApp:VerifyToken"] = "credix-test-verify",
                        ["WhatsApp:AppSecret"] = "",
                        ["Hosting:DisableHttpsRedirection"] = "true",
                    });
            });
    }
}
