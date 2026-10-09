namespace Credito.Modern.Application.CreditoPlanes;

/// <param name="CreditoIds">Créditos seleccionados (máx. 25).</param>
/// <param name="UsuarioId">Gestor de la cartera (sesión de caja). Roles elevados pueden elegir otro gestor.</param>
/// <param name="OficinaId">Oficina de la sesión de caja.</param>
public sealed record GenerarRutaCobrosRequest(
    int[] CreditoIds,
    int? UsuarioId = null,
    int? OficinaId = null);

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
