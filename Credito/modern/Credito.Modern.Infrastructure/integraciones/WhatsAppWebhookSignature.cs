using System.Security.Cryptography;
using System.Text;

namespace Credito.Modern.Infrastructure.Integraciones;

/// <summary>Valida <c>X-Hub-Signature-256</c> de Meta (HMAC-SHA256 del body con App Secret).</summary>
public static class WhatsAppWebhookSignature
{
    public static bool EsValida(string? header, ReadOnlySpan<byte> body, string? appSecret)
    {
        if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        const string prefix = "sha256=";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hex = header[prefix.Length..].Trim();
        if (hex.Length == 0 || hex.Length % 2 != 0)
        {
            return false;
        }

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return false;
        }

        var key = Encoding.UTF8.GetBytes(appSecret.Trim());
        var actual = HMACSHA256.HashData(key, body);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
