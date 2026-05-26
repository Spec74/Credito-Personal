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

        var cartera = await cobroDiario.ListarAsync(usuarioId, oficinaId, ct).ConfigureAwait(false);
        var clientesRuta = cartera.Where(x => ids.Contains(x.CreditoId)).ToList();
        if (clientesRuta.Count == 0)
            return new GenerarRutaCobrosResponse(false, null, "Ningún crédito pertenece a su cartera del día.");

        var gpsRows = await ObtenerGpsClientesAsync(ids, ct).ConfigureAwait(false);
        var (latOficina, lonOficina) = await ObtenerGpsOficinaAsync(oficinaId, ct).ConfigureAwait(false);

        var clientesConGps = clientesRuta.Select(c =>
        {
            gpsRows.TryGetValue(c.CreditoId, out var gps);
            var tieneGps = gps.Latitud is not null && gps.Latitud != 0;
            return new ClienteRutaItem(
                c,
                tieneGps ? gps.Latitud!.Value : 0,
                tieneGps ? gps.Longitud ?? 0 : 0,
                tieneGps);
        }).ToList();

        var pendientes = clientesConGps.Where(x => x.TieneGps).ToList();
        var sinGps = clientesConGps.Where(x => !x.TieneGps).ToList();
        var rutaOrdenada = new List<ClienteRutaItem>();

        if (pendientes.Count > 0)
        {
            var primer = pendientes
                .OrderBy(p => Distancia(latOficina, lonOficina, p.Lat, p.Lon))
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

        var texto = ConstruirTextoWhatsApp(rutaOrdenada);
        var idUnico = Guid.NewGuid().ToString("N");
        cache.Set(idUnico, texto, CacheTtl);
        return new GenerarRutaCobrosResponse(true, $"/api/v1/credito/ruta-wa/{idUnico}", null);
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
