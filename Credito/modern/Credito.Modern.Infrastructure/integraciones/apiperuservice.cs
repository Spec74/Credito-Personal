using System.Net.Http.Headers;
using System.Text.Json;
using Credito.Modern.Application.Integraciones;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Integraciones;

public sealed class ApiPeruService(IHttpClientFactory httpClientFactory, IOptions<ApiPeruOptions> options)
    : IApiPeruService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly ApiPeruOptions _opts = options.Value;

    public async Task<ApiPeruDniDto> ConsultarDniAsync(
        string dni,
        CancellationToken cancellationToken = default)
    {
        var doc = dni.Trim();
        if (doc.Length != 8 || !doc.All(char.IsDigit))
        {
            return new ApiPeruDniDto(false, null, null, null, "DNI debe tener 8 dígitos.");
        }

        var envelope = await FetchEnvelopeAsync<DniPayload>($"dni/{doc}", cancellationToken)
            .ConfigureAwait(false);
        if (!envelope.Success || envelope.Data is null)
        {
            return new ApiPeruDniDto(
                false,
                null,
                null,
                null,
                envelope.Message ?? "DNI no encontrado en RENIEC.");
        }

        return new ApiPeruDniDto(
            true,
            envelope.Data.Nombres,
            envelope.Data.ApellidoPaterno,
            envelope.Data.ApellidoMaterno,
            null);
    }

    public async Task<ApiPeruRucDto> ConsultarRucAsync(
        string ruc,
        CancellationToken cancellationToken = default)
    {
        var doc = ruc.Trim();
        if (doc.Length != 11 || !doc.All(char.IsDigit))
        {
            return new ApiPeruRucDto(false, null, null, "RUC debe tener 11 dígitos.");
        }

        var envelope = await FetchEnvelopeAsync<RucPayload>($"ruc/{doc}", cancellationToken)
            .ConfigureAwait(false);
        if (!envelope.Success || envelope.Data is null)
        {
            return new ApiPeruRucDto(
                false,
                null,
                null,
                envelope.Message ?? "RUC no encontrado en SUNAT.");
        }

        return new ApiPeruRucDto(
            true,
            envelope.Data.NombreORazonSocial,
            envelope.Data.Direccion,
            null);
    }

    private async Task<ApiPeruEnvelope<T>> FetchEnvelopeAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_opts.Token))
        {
            throw new InvalidOperationException(
                "Configure ApiPeru:Token (user-secrets o variables de entorno).");
        }

        var client = httpClientFactory.CreateClient(nameof(ApiPeruService));
        var baseUrl = _opts.BaseUrl.TrimEnd('/') + "/";
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), path));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", NormalizeToken(_opts.Token));

        using var res = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var json = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"ApiPeru respondió {(int)res.StatusCode}.");
        }

        var envelope = JsonSerializer.Deserialize<ApiPeruEnvelope<T>>(json, JsonOpts);
        return envelope ?? new ApiPeruEnvelope<T> { Success = false, Message = "Respuesta inválida." };
    }

    private static string NormalizeToken(string raw)
    {
        var t = raw.Trim();
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            t = t["Bearer ".Length..].Trim();
        }

        return t;
    }

    private sealed class ApiPeruEnvelope<T>
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public T? Data { get; init; }
    }

    private sealed class DniPayload
    {
        public string? Nombres { get; init; }
        public string? ApellidoPaterno { get; init; }
        public string? ApellidoMaterno { get; init; }
    }

    private sealed class RucPayload
    {
        public string? NombreORazonSocial { get; init; }
        public string? Direccion { get; init; }
    }
}
