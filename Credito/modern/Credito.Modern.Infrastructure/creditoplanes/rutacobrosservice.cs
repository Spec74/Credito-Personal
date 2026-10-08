using System.Globalization;
using System.Text;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RutaCobrosService(
    IOptions<SqlDatabaseOptions> options,
    IRptCobroDiarioReadService cobroDiario,
    IMemoryCache cache) : IRutaCobrosService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(4);
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<GenerarRutaCobrosResponse> GenerarAsync(
        int usuarioId,
        int oficinaId,
        IReadOnlyList<int> creditoIds,
        CancellationToken ct = default)
    {
        if (creditoIds.Count == 0)
            return new GenerarRutaCobrosResponse(false, null, "No se recibieron clientes.");

        Ensure();
        var ids = creditoIds.Distinct().Where(id => id > 0).ToList();
        if (ids.Count == 0)
            return new GenerarRutaCobrosResponse(false, null, "No se recibieron clientes.");

        if (ids.Count > 25)
            return new GenerarRutaCobrosResponse(false, null, "Máximo 25 créditos por ruta.");

        var cartera = await cobroDiario.ListarAsync(usuarioId, oficinaId, ct).ConfigureAwait(false);
        var clientesRuta = cartera.Where(x => ids.Contains(x.CreditoId)).ToList();
        if (clientesRuta.Count == 0)
            return new GenerarRutaCobrosResponse(false, null, "Ningún crédito pertenece a su cartera del día.");

        var gpsRows = await ObtenerGpsClientesAsync(ids, ct).ConfigureAwait(false);
        var (latOficina, lonOficina) = await ObtenerGpsOficinaAsync(oficinaId, ct).ConfigureAwait(false);
        var oficinaTieneGps = latOficina != 0 && lonOficina != 0;

        var clientesConGps = clientesRuta.Select(c =>
        {
            gpsRows.TryGetValue(c.CreditoId, out var gps);
            var tieneGps = gps.Latitud is not null && gps.Latitud != 0
                && gps.Longitud is not null && gps.Longitud != 0;
            return new ClienteRutaItem(
                c,
                tieneGps ? gps.Latitud!.Value : 0,
                tieneGps ? gps.Longitud!.Value : 0,
                tieneGps);
        }).ToList();

        var pendientes = clientesConGps.Where(x => x.TieneGps).ToList();
        var sinGps = clientesConGps.Where(x => !x.TieneGps).ToList();
        var rutaOrdenada = new List<ClienteRutaItem>();

        if (pendientes.Count > 0)
        {
            var origenLat = oficinaTieneGps ? latOficina : pendientes[0].Lat;
            var origenLon = oficinaTieneGps ? lonOficina : pendientes[0].Lon;
            var primer = pendientes
                .OrderBy(p => Distancia(origenLat, origenLon, p.Lat, p.Lon))
                .First();
            rutaOrdenada.Add(primer);
            pendientes.Remove(primer);
            var actual = primer;
            while (pendientes.Count > 0)
            {
                var masCercano = pendientes
                    .OrderBy(p => Distancia(actual.Lat, actual.Lon, p.Lat, p.Lon))
                    .First();
                rutaOrdenada.Add(masCercano);
                pendientes.Remove(masCercano);
                actual = masCercano;
            }
        }

        rutaOrdenada.AddRange(sinGps);

        var paradas = new List<RutaCobroParadaDto>();
        var orden = 1;
        foreach (var item in rutaOrdenada)
        {
            paradas.Add(new RutaCobroParadaDto(
                orden,
                item.Datos.CreditoId,
                item.Datos.Cliente ?? $"Crédito {item.Datos.CreditoId}",
                item.Datos.Saldo ?? 0m,
                item.Datos.Direccion,
                item.TieneGps ? item.Lat : null,
                item.TieneGps ? item.Lon : null,
                item.TieneGps));
            orden++;
        }

        var urlNav = ConstruirUrlNavegacionGoogle(
            oficinaTieneGps ? latOficina : null,
            oficinaTieneGps ? lonOficina : null,
            paradas);

        var texto = ConstruirTextoWhatsApp(rutaOrdenada);
        var idUnico = Guid.NewGuid().ToString("N");
        cache.Set(idUnico, texto, CacheTtl);

        return new GenerarRutaCobrosResponse(
            true,
            $"/api/v1/credito/ruta-wa/{idUnico}",
            null,
            urlNav,
            oficinaTieneGps ? latOficina : null,
            oficinaTieneGps ? lonOficina : null,
            paradas);
    }

    public string? ObtenerTextoRuta(string cacheId) =>
        string.IsNullOrWhiteSpace(cacheId) ? null : cache.Get<string>(cacheId);

    private async Task<Dictionary<int, (decimal? Latitud, decimal? Longitud)>> ObtenerGpsClientesAsync(
        List<int> creditoIds,
        CancellationToken ct)
    {
        const string sql = """
            SELECT c.CreditoId,
                   cl.Latitud,
                   cl.Longitud
            FROM CREDITO.Credito AS c
            INNER JOIN MAESTRO.Cliente AS cl ON cl.PersonaId = c.PersonaId
            WHERE c.CreditoId IN @CreditoIds;
            """;
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<(int CreditoId, decimal? Latitud, decimal? Longitud)>(
            new CommandDefinition(sql, new { CreditoIds = creditoIds }, cancellationToken: ct))
            .ConfigureAwait(false);
        return rows.ToDictionary(x => x.CreditoId, x => (x.Latitud, x.Longitud));
    }

    private async Task<(decimal Lat, decimal Lon)> ObtenerGpsOficinaAsync(int oficinaId, CancellationToken ct)
    {
        const string sql = """
            SELECT Latitud, Longitud
            FROM MAESTRO.Oficina
            WHERE OficinaId = @OficinaId;
            """;
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        var row = await conn.QuerySingleOrDefaultAsync<(decimal? Latitud, decimal? Longitud)>(
            new CommandDefinition(sql, new { OficinaId = oficinaId }, cancellationToken: ct))
            .ConfigureAwait(false);
        return (
            row.Latitud is not null ? row.Latitud.Value : 0,
            row.Longitud is not null ? row.Longitud.Value : 0);
    }

    private static double Distancia(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        var dlat = (double)(lat1 - lat2);
        var dlon = (double)(lon1 - lon2);
        return Math.Sqrt(dlat * dlat + dlon * dlon);
    }

    /// <summary>
    /// URL multi-parada para abrir Google Maps en el celular (navegación turn-by-turn).
    /// </summary>
    public static string? ConstruirUrlNavegacionGoogle(
        decimal? latOrigen,
        decimal? lonOrigen,
        IReadOnlyList<RutaCobroParadaDto> paradas)
    {
        var conGps = paradas.Where(p => p.TieneGps && p.Latitud is not null && p.Longitud is not null).ToList();
        if (conGps.Count == 0)
            return null;

        static string Coord(decimal lat, decimal lon) =>
            string.Create(CultureInfo.InvariantCulture, $"{lat},{lon}");

        var origin = latOrigen is not null && lonOrigen is not null && latOrigen != 0 && lonOrigen != 0
            ? Coord(latOrigen.Value, lonOrigen.Value)
            : Coord(conGps[0].Latitud!.Value, conGps[0].Longitud!.Value);

        var destino = conGps[^1];
        var destination = Coord(destino.Latitud!.Value, destino.Longitud!.Value);

        var sb = new StringBuilder("https://www.google.com/maps/dir/?api=1");
        sb.Append(CultureInfo.InvariantCulture, $"&origin={Uri.EscapeDataString(origin)}");
        sb.Append(CultureInfo.InvariantCulture, $"&destination={Uri.EscapeDataString(destination)}");
        sb.Append("&travelmode=driving");

        if (conGps.Count > 1)
        {
            var middles = conGps.Take(conGps.Count - 1);
            // Si el origen es la oficina, incluir el primer cliente en waypoints;
            // si el origen es el primer cliente, omitirlo de waypoints.
            var origenEsOficina = latOrigen is not null && lonOrigen is not null && latOrigen != 0;
            var waypoints = origenEsOficina
                ? middles
                : middles.Skip(1);
            var wp = string.Join('|', waypoints.Select(p => Coord(p.Latitud!.Value, p.Longitud!.Value)));
            if (wp.Length > 0)
                sb.Append(CultureInfo.InvariantCulture, $"&waypoints={Uri.EscapeDataString(wp)}");
        }

        return sb.ToString();
    }

    private static string ConstruirTextoWhatsApp(List<ClienteRutaItem> rutaOrdenada)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<== RUTA DE COBRANZA ==>");
        sb.AppendLine("-----------------------------------");
        var orden = 1;
        foreach (var item in rutaOrdenada)
        {
            var c = item.Datos;
            sb.AppendLine(CultureInfo.InvariantCulture, $"*{orden}. {c.Cliente}*");
            sb.AppendLine(CultureInfo.InvariantCulture, $"   > Cobrar: S/ {c.Saldo}");
            if (item.TieneGps)
            {
                var lat = item.Lat.ToString(CultureInfo.InvariantCulture);
                var lon = item.Lon.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine(CultureInfo.InvariantCulture, $"   > Mapa: https://maps.google.com/?q={lat},{lon}");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"   > Dir: {c.Direccion} (Sin GPS)");
            }

            sb.AppendLine();
            orden++;
        }

        return sb.ToString();
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }

    private sealed record ClienteRutaItem(
        RptCobroDiarioRowDto Datos,
        decimal Lat,
        decimal Lon,
        bool TieneGps);
}
