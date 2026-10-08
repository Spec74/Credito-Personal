namespace Credito.Modern.Application.CreditoPlanes;

public sealed record GenerarRutaCobrosRequest(int[] CreditoIds);

/// <summary>Parada ordenada para mapa in-app y navegación.</summary>
public sealed record RutaCobroParadaDto(
    int Orden,
    int CreditoId,
    string Cliente,
    decimal MontoCobrar,
    string? Direccion,
    decimal? Latitud,
    decimal? Longitud,
    bool TieneGps);

/// <summary>
/// Respuesta de ruta: mapa in-app (paradas + origen) + navegación Google multi-parada
/// + enlace WhatsApp opcional (secundario).
/// </summary>
public sealed record GenerarRutaCobrosResponse(
    bool Exito,
    string? UrlCortita,
    string? Mensaje,
    string? UrlNavegacionGoogle = null,
    decimal? LatitudOrigen = null,
    decimal? LongitudOrigen = null,
    IReadOnlyList<RutaCobroParadaDto>? Paradas = null);
