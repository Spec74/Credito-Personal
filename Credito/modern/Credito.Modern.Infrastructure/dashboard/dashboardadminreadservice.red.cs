using Credito.Modern.Application.Dashboard;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace Credito.Modern.Infrastructure.Dashboard;

/// <summary>
/// Tablero Red: un solo roundtrip multi-oficina (STRING_SPLIT), sin fan-out N×detalle.
/// </summary>
public sealed partial class DashboardAdminReadService
{
    private const int RedCommandTimeoutSeconds = 45;

    private static readonly string SqlRed = """
        DECLARE @Hoy date = dbo.ufnFecha();
        DECLARE @Ayer date = DATEADD(DAY, -1, @Hoy);
        DECLARE @Manana date = DATEADD(DAY, 1, @Hoy);
        DECLARE @LimiteVencer date = DATEADD(DAY, 8, @Hoy);

        DECLARE @Ids TABLE (OficinaId int NOT NULL PRIMARY KEY);
        INSERT INTO @Ids (OficinaId)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS int)
        FROM STRING_SPLIT(@OficinaIdsCsv, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS int) >= 1;

        ;WITH Cob AS (
            SELECT
                c.OficinaId,
                ISNULL(SUM(CASE WHEN m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END), 0) AS CobradoHoy,
                ISNULL(SUM(CASE WHEN m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END), 0) AS CobradoAyer
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.Credito AS c ON c.CreditoId = m.CreditoId
            INNER JOIN @Ids AS i ON i.OficinaId = c.OficinaId
            WHERE m.Operacion = 'CUO'
              AND m.Estado = 1
              AND m.ImportePago > 0
              AND m.FechaReg >= @Ayer
              AND m.FechaReg < @Manana
            GROUP BY c.OficinaId
        ),
        Coloc AS (
            SELECT
                c.OficinaId,
                ISNULL(SUM(CASE WHEN c.FechaDesembolso >= @Hoy AND c.FechaDesembolso < @Manana THEN c.MontoDesembolso ELSE 0 END), 0) AS DesembolsoHoy,
                ISNULL(SUM(CASE WHEN c.FechaDesembolso >= @Ayer AND c.FechaDesembolso < @Hoy THEN c.MontoDesembolso ELSE 0 END), 0) AS DesembolsoAyer
            FROM CREDITO.Credito AS c
            INNER JOIN @Ids AS i ON i.OficinaId = c.OficinaId
            WHERE c.FechaDesembolso IS NOT NULL
              AND c.FechaDesembolso >= @Ayer
              AND c.FechaDesembolso < @Manana
              AND c.Estado IN ('DES', 'PAG', 'REP')
            GROUP BY c.OficinaId
        ),
        Flujo AS (
            SELECT
                ca.OficinaId,
                ISNULL(SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END), 0) AS EntradasHoy,
                ISNULL(SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END), 0) AS SalidasHoy,
                ISNULL(SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END), 0) AS EntradasAyer,
                ISNULL(SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END), 0) AS SalidasAyer
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = m.CajaDiarioId
            INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
            INNER JOIN @Ids AS i ON i.OficinaId = ca.OficinaId
            WHERE m.Estado = 1
              AND m.FechaReg >= @Ayer
              AND m.FechaReg < @Manana
            GROUP BY ca.OficinaId
        ),
        CreditosActivos AS (
            SELECT c.OficinaId, c.CreditoId, c.PersonaId, c.FechaVencimiento
            FROM CREDITO.Credito AS c
            INNER JOIN @Ids AS i ON i.OficinaId = c.OficinaId
            WHERE c.Estado = 'DES'
              AND c.IndIrrecuperable = 0
        ),
        MoraPersona AS (
            SELECT DISTINCT ca.OficinaId, ca.PersonaId
            FROM CreditosActivos AS ca
            INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = ca.CreditoId
            WHERE pp.Estado = 'PEN'
              AND pp.FechaVencimiento < @Hoy
        ),
        Cartera AS (
            SELECT
                ca.OficinaId,
                COUNT(DISTINCT ca.PersonaId) AS TotalClientes,
                COUNT(DISTINCT mp.PersonaId) AS ClientesMora,
                COUNT(DISTINCT CASE
                    WHEN ca.FechaVencimiento >= @Hoy AND ca.FechaVencimiento < @LimiteVencer
                    THEN ca.CreditoId
                END) AS CreditosPorVencerSemana
            FROM CreditosActivos AS ca
            LEFT JOIN MoraPersona AS mp
                ON mp.OficinaId = ca.OficinaId
               AND mp.PersonaId = ca.PersonaId
            GROUP BY ca.OficinaId
        ),
        Analistas AS (
            SELECT
                ur.OficinaId,
                COUNT(DISTINCT u.UsuarioId) AS TotalAnalistas
            FROM MAESTRO.UsuarioRol AS ur
            INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
            INNER JOIN @Ids AS i ON i.OficinaId = ur.OficinaId
            WHERE r.Denominacion = N'ANALISTA'
              AND r.Estado = CAST(1 AS bit)
              AND u.Estado = CAST(1 AS bit)
              AND u.NombreUsuario <> N'IRRECUPERABLE'
            GROUP BY ur.OficinaId
        )
        SELECT
            i.OficinaId,
            ISNULL(NULLIF(LTRIM(RTRIM(o.Denominacion)), N''), CONCAT(N'Sede ', i.OficinaId)) AS NombreOficina,
            CAST(@Hoy AS datetime) AS FechaConsulta,
            ISNULL(cob.CobradoHoy, 0) AS CobradoHoy,
            ISNULL(cob.CobradoAyer, 0) AS CobradoAyer,
            ISNULL(col.DesembolsoHoy, 0) AS DesembolsoHoy,
            ISNULL(col.DesembolsoAyer, 0) AS DesembolsoAyer,
            ISNULL(fl.EntradasHoy, 0) - ISNULL(fl.SalidasHoy, 0) AS FlujoNetoHoy,
            ISNULL(fl.EntradasAyer, 0) - ISNULL(fl.SalidasAyer, 0) AS FlujoNetoAyer,
            ISNULL(car.TotalClientes, 0) AS TotalClientes,
            ISNULL(car.ClientesMora, 0) AS ClientesMora,
            ISNULL(car.CreditosPorVencerSemana, 0) AS CreditosPorVencerSemana,
            ISNULL(an.TotalAnalistas, 0) AS TotalAnalistas
        FROM @Ids AS i
        LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = i.OficinaId
        LEFT JOIN Cob AS cob ON cob.OficinaId = i.OficinaId
        LEFT JOIN Coloc AS col ON col.OficinaId = i.OficinaId
        LEFT JOIN Flujo AS fl ON fl.OficinaId = i.OficinaId
        LEFT JOIN Cartera AS car ON car.OficinaId = i.OficinaId
        LEFT JOIN Analistas AS an ON an.OficinaId = i.OficinaId
        ORDER BY ISNULL(cob.CobradoHoy, 0) DESC, NombreOficina;
        """;

    public async Task<DashboardAdminRedDto> ObtenerRedAsync(
        IReadOnlyList<(int OficinaId, string Nombre)> sedes,
        CancellationToken cancellationToken = default)
    {
        if (sedes is null || sedes.Count == 0)
        {
            throw new ArgumentException("Debe indicar al menos una sede.", nameof(sedes));
        }

        var unique = sedes
            .Where(s => s.OficinaId >= 1)
            .GroupBy(s => s.OficinaId)
            .Select(g => g.First())
            .OrderBy(s => s.OficinaId)
            .ToList();

        if (unique.Count == 0)
        {
            throw new ArgumentException("Debe indicar al menos una sede válida.", nameof(sedes));
        }

        var idsKey = string.Join(',', unique.Select(s => s.OficinaId));
        var cacheKey = $"dashboard:admin:red:v2:{idsKey}";
        if (cache.TryGetValue(cacheKey, out DashboardAdminRedDto? cached) && cached is not null)
        {
            return cached;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var raw = (await connection.QueryAsync<RedSedeSqlRow>(
            new CommandDefinition(
                SqlRed,
                new { OficinaIdsCsv = idsKey },
                commandTimeout: RedCommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).AsList();

        var byId = raw.ToDictionary(r => r.OficinaId);
        var nameFallback = unique.ToDictionary(
            s => s.OficinaId,
            s => string.IsNullOrWhiteSpace(s.Nombre) ? $"Sede {s.OficinaId}" : s.Nombre.Trim());

        var rows = unique
            .Select(s =>
            {
                byId.TryGetValue(s.OficinaId, out var sql);
                return MapRedSedeRow(s.OficinaId, nameFallback[s.OficinaId], sql);
            })
            .OrderByDescending(r => r.CobradoHoy)
            .ThenBy(r => r.NombreOficina, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cobradoHoy = rows.Sum(r => r.CobradoHoy);
        var cobradoAyer = rows.Sum(r => r.CobradoAyer);
        var desembolsoHoy = rows.Sum(r => r.DesembolsoHoy);
        var desembolsoAyer = raw.Sum(r => r.DesembolsoAyer);
        var flujoHoy = rows.Sum(r => r.FlujoNetoHoy);
        var flujoAyer = raw.Sum(r => r.FlujoNetoAyer);
        var clientesMora = rows.Sum(r => r.ClientesMora);
        var porVencer = rows.Sum(r => r.CreditosPorVencerSemana);
        var analistas = rows.Sum(r => r.TotalAnalistas);
        var clientes = rows.Sum(r => r.TotalClientes);
        var fecha = raw.Select(r => r.FechaConsulta).DefaultIfEmpty(DateTime.Today).Max();

        var moraRed = clientes > 0
            ? Math.Round((clientesMora / (decimal)clientes) * 100m, 1, MidpointRounding.AwayFromZero)
            : 0m;

        var dto = new DashboardAdminRedDto(
            FechaConsulta: fecha,
            TotalSedes: rows.Count,
            CobradoHoy: cobradoHoy,
            CobradoAyer: cobradoAyer,
            VariacionCobradoHoyPct: DashboardAnalistaInsights.VariacionPorcentaje(cobradoHoy, cobradoAyer),
            DesembolsoHoy: desembolsoHoy,
            DesembolsoAyer: desembolsoAyer,
            FlujoNetoHoy: flujoHoy,
            FlujoNetoAyer: flujoAyer,
            PorcentajeMoraRed: moraRed,
            ClientesMora: clientesMora,
            CreditosPorVencerSemana: porVencer,
            TotalAnalistas: analistas,
            TotalClientes: clientes,
            SedesEnRojo: rows.Count(r => r.Semaforo == "rojo"),
            SedesEnAmbar: rows.Count(r => r.Semaforo == "ambar"),
            SedesEnVerde: rows.Count(r => r.Semaforo == "verde"),
            Sedes: rows);

        cache.Set(cacheKey, dto, ShellCacheTtl);
        return dto;
    }

    private static DashboardAdminRedSedeRowDto MapRedSedeRow(
        int oficinaId,
        string nombreFallback,
        RedSedeSqlRow? sql)
    {
        var nombre = sql is null || string.IsNullOrWhiteSpace(sql.NombreOficina)
            ? nombreFallback
            : sql.NombreOficina.Trim();
        var cobradoHoy = sql?.CobradoHoy ?? 0m;
        var cobradoAyer = sql?.CobradoAyer ?? 0m;
        var desembolsoHoy = sql?.DesembolsoHoy ?? 0m;
        var flujoHoy = sql?.FlujoNetoHoy ?? 0m;
        var totalClientes = sql?.TotalClientes ?? 0;
        var clientesMora = sql?.ClientesMora ?? 0;
        var pctMora = totalClientes > 0
            ? Math.Round((clientesMora / (decimal)totalClientes) * 100m, 1, MidpointRounding.AwayFromZero)
            : 0m;
        var (semaforo, motivo) = EvaluarSemaforo(pctMora, flujoHoy, cobradoHoy, cobradoAyer);

        return new DashboardAdminRedSedeRowDto(
            OficinaId: oficinaId,
            NombreOficina: nombre,
            CobradoHoy: cobradoHoy,
            CobradoAyer: cobradoAyer,
            VariacionCobradoHoyPct: DashboardAnalistaInsights.VariacionPorcentaje(cobradoHoy, cobradoAyer),
            DesembolsoHoy: desembolsoHoy,
            FlujoNetoHoy: flujoHoy,
            PorcentajeMora: pctMora,
            ClientesMora: clientesMora,
            CreditosPorVencerSemana: sql?.CreditosPorVencerSemana ?? 0,
            TotalAnalistas: sql?.TotalAnalistas ?? 0,
            TotalClientes: totalClientes,
            Semaforo: semaforo,
            MotivoSemaforo: motivo);
    }

    private static (string Semaforo, string Motivo) EvaluarSemaforo(
        decimal pctMora,
        decimal flujoNetoHoy,
        decimal cobradoHoy,
        decimal cobradoAyer)
    {
        if (pctMora >= 20m)
        {
            return ("rojo", $"Mora {pctMora:0.#}% (≥ 20%)");
        }

        if (flujoNetoHoy < 0m && cobradoAyer > 0m && cobradoHoy < cobradoAyer * 0.7m)
        {
            return ("rojo", "Flujo negativo y cobranza bajo 70% vs ayer");
        }

        if (flujoNetoHoy < 0m)
        {
            return ("ambar", "Flujo neto del día negativo");
        }

        if (pctMora >= 10m)
        {
            return ("ambar", $"Mora {pctMora:0.#}% (≥ 10%)");
        }

        if (cobradoAyer > 0m && cobradoHoy < cobradoAyer)
        {
            return ("ambar", "Cobranza bajo el ritmo de ayer");
        }

        return ("verde", "Operación dentro de umbrales");
    }

    private sealed class RedSedeSqlRow
    {
        public int OficinaId { get; init; }
        public string NombreOficina { get; init; } = "";
        public DateTime FechaConsulta { get; init; }
        public decimal CobradoHoy { get; init; }
        public decimal CobradoAyer { get; init; }
        public decimal DesembolsoHoy { get; init; }
        public decimal DesembolsoAyer { get; init; }
        public decimal FlujoNetoHoy { get; init; }
        public decimal FlujoNetoAyer { get; init; }
        public int TotalClientes { get; init; }
        public int ClientesMora { get; init; }
        public int CreditosPorVencerSemana { get; init; }
        public int TotalAnalistas { get; init; }
    }
}
