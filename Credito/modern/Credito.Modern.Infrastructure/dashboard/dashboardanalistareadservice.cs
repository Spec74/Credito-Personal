using System.Data;
using Credito.Modern.Application.Dashboard;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Dashboard;

/// <summary>
/// Tablero del analista: KPIs + productividad en un roundtrip (semántica de
/// <c>usp_DashboardGestor</c> / productividad). Mora clasificada igual que el SP.
/// Drill-down llama <c>usp_DashboardGestorClientesMora</c>. Ranking/podio omitidos
/// (retirados en producción).
/// </summary>
public sealed class DashboardAnalistaReadService(
    IOptions<SqlDatabaseOptions> options,
    IMemoryCache cache) : IDashboardAnalistaReadService
{
    private const int CommandTimeoutSeconds = 60;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(90);

    private static readonly string Sql = """
        DECLARE @Hoy date = dbo.ufnFecha();
        DECLARE @Manana date = DATEADD(DAY, 1, @Hoy);
        DECLARE @Ayer date = DATEADD(DAY, -1, @Hoy);
        DECLARE @InicioMes date = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);
        DECLARE @InicioMesAnterior date = DATEADD(MONTH, -1, @InicioMes);
        DECLARE @InicioProd date = DATEADD(DAY, -29, @Hoy);
        DECLARE @LimiteVencer date = DATEADD(DAY, 8, @Hoy);

        IF OBJECT_ID('tempdb..#Creditos') IS NOT NULL DROP TABLE #Creditos;
        IF OBJECT_ID('tempdb..#MoraPersona') IS NOT NULL DROP TABLE #MoraPersona;

        CREATE TABLE #Creditos (
            CreditoId int NOT NULL PRIMARY KEY,
            PersonaId int NOT NULL,
            Estado char(3) NOT NULL,
            IndIrrecuperable bit NOT NULL,
            FechaDesembolso datetime NULL,
            FechaVencimiento date NOT NULL
        );

        INSERT INTO #Creditos (CreditoId, PersonaId, Estado, IndIrrecuperable, FechaDesembolso, FechaVencimiento)
        SELECT c.CreditoId,
               c.PersonaId,
               c.Estado,
               c.IndIrrecuperable,
               c.FechaDesembolso,
               c.FechaVencimiento
        FROM CREDITO.Credito AS c
        WHERE c.UsuarioRegId = @UsuarioId
          AND c.OficinaId = @OficinaId
          AND c.FechaDesembolso IS NOT NULL
          AND c.FechaDesembolso < @Manana;

        ;WITH CreditosActivos AS (
            SELECT CreditoId, PersonaId
            FROM #Creditos
            WHERE Estado = 'DES'
              AND IndIrrecuperable = 0
        ),
        PlanAgregado AS (
            SELECT pp.CreditoId,
                   SUM(ISNULL(pp.Cuota, 0) + ISNULL(pp.Cargo, 0)) AS MontoPlan,
                   MAX(CASE WHEN pp.Estado = 'PEN' AND pp.FechaVencimiento < @Hoy THEN 1 ELSE 0 END) AS TieneMora,
                   MIN(CASE WHEN pp.Estado = 'PEN' AND pp.FechaVencimiento < @Hoy THEN pp.FechaVencimiento END) AS PrimeraCuotaVencida
            FROM CREDITO.PlanPago AS pp
            INNER JOIN CreditosActivos AS ca ON ca.CreditoId = pp.CreditoId
            GROUP BY pp.CreditoId
        ),
        PagosAgregados AS (
            SELECT m.CreditoId,
                   SUM(m.ImportePago) AS TotalPagado,
                   MAX(m.FechaReg) AS FechaUltimoPago
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CreditosActivos AS ca ON ca.CreditoId = m.CreditoId
            WHERE m.Operacion = 'CUO'
              AND m.Estado = 1
              AND m.ImportePago > 0
              AND m.FechaReg < @Manana
            GROUP BY m.CreditoId
        ),
        CarteraCredito AS (
            SELECT ca.CreditoId,
                   ca.PersonaId,
                   ISNULL(pa.TieneMora, 0) AS TieneMora,
                   pa.PrimeraCuotaVencida,
                   CAST(CASE
                       WHEN ISNULL(pa.MontoPlan, 0) - ISNULL(pg.TotalPagado, 0) > 0
                       THEN ISNULL(pa.MontoPlan, 0) - ISNULL(pg.TotalPagado, 0)
                       ELSE 0 END AS decimal(18, 2)) AS Saldo,
                   CAST(ISNULL(pg.TotalPagado, 0) AS decimal(18, 2)) AS TotalPagado,
                   pg.FechaUltimoPago
            FROM CreditosActivos AS ca
            LEFT JOIN PlanAgregado AS pa ON pa.CreditoId = ca.CreditoId
            LEFT JOIN PagosAgregados AS pg ON pg.CreditoId = ca.CreditoId
        )
        SELECT PersonaId,
               SUM(Saldo) AS SaldoTotal,
               SUM(CASE WHEN TieneMora = 1 THEN Saldo ELSE 0 END) AS SaldoMora,
               MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN 1 ELSE 0 END) AS TieneMora,
               SUM(CASE WHEN TieneMora = 1 THEN TotalPagado ELSE 0 END) AS PagadoEnCreditosMora,
               MAX(CASE
                   WHEN TieneMora = 1 AND Saldo > 0 AND FechaUltimoPago >= PrimeraCuotaVencida
                   THEN 1 ELSE 0 END) AS PagoDesdeInicioMora
        INTO #MoraPersona
        FROM CarteraCredito
        GROUP BY PersonaId;

        SELECT ISNULL((
                   SELECT ISNULL(p.NombreCompleto, u.NombreUsuario)
                   FROM MAESTRO.Usuario AS u
                   LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
                   WHERE u.UsuarioId = @UsuarioId
               ), N'Analista') AS NombreAnalista,
               CAST(@Hoy AS datetime) AS FechaConsulta,
               (
                   SELECT COUNT(DISTINCT cr.PersonaId)
                   FROM #Creditos AS cr
                   WHERE cr.Estado = 'DES'
                     AND cr.IndIrrecuperable = 0
               ) AS TotalClientes,
               (
                   SELECT COUNT(*)
                   FROM (
                       SELECT cr.PersonaId,
                              MIN(cr.FechaDesembolso) AS PrimeraFecha
                       FROM #Creditos AS cr
                       WHERE cr.Estado IN ('DES', 'PAG', 'REP')
                       GROUP BY cr.PersonaId
                   ) AS primera
                   WHERE primera.PrimeraFecha >= @InicioMes
                     AND primera.PrimeraFecha < @Manana
               ) AS ClientesNuevosActual,
               (
                   SELECT COUNT(*)
                   FROM (
                       SELECT cr.PersonaId,
                              MIN(cr.FechaDesembolso) AS PrimeraFecha
                       FROM #Creditos AS cr
                       WHERE cr.Estado IN ('DES', 'PAG', 'REP')
                       GROUP BY cr.PersonaId
                   ) AS primera
                   WHERE primera.PrimeraFecha >= @InicioMesAnterior
                     AND primera.PrimeraFecha < @InicioMes
               ) AS ClientesNuevosAnterior,
               (
                   SELECT COUNT(*)
                   FROM #Creditos AS cr
                   WHERE cr.Estado IN ('DES', 'PAG', 'REP')
                     AND cr.FechaDesembolso >= @InicioMes
                     AND cr.FechaDesembolso < @Manana
               ) AS CreditosActual,
               (
                   SELECT COUNT(*)
                   FROM #Creditos AS cr
                   WHERE cr.Estado IN ('DES', 'PAG', 'REP')
                     AND cr.FechaDesembolso >= @InicioMesAnterior
                     AND cr.FechaDesembolso < @InicioMes
               ) AS CreditosAnterior,
               ISNULL((
                   SELECT SUM(m.ImportePago)
                   FROM CREDITO.MovimientoCaja AS m
                   INNER JOIN #Creditos AS cr ON cr.CreditoId = m.CreditoId
                   WHERE m.Operacion = 'CUO'
                     AND m.Estado = 1
                     AND m.ImportePago > 0
                     AND m.FechaReg >= @InicioMes
                     AND m.FechaReg < @Manana
               ), 0) AS CobradoActual,
               ISNULL((
                   SELECT SUM(m.ImportePago)
                   FROM CREDITO.MovimientoCaja AS m
                   INNER JOIN #Creditos AS cr ON cr.CreditoId = m.CreditoId
                   WHERE m.Operacion = 'CUO'
                     AND m.Estado = 1
                     AND m.ImportePago > 0
                     AND m.FechaReg >= @InicioMesAnterior
                     AND m.FechaReg < @InicioMes
               ), 0) AS CobradoAnterior,
               ISNULL((
                   SELECT SUM(m.ImportePago)
                   FROM CREDITO.MovimientoCaja AS m
                   INNER JOIN #Creditos AS cr ON cr.CreditoId = m.CreditoId
                   WHERE m.Operacion = 'CUO'
                     AND m.Estado = 1
                     AND m.ImportePago > 0
                     AND m.FechaReg >= @Hoy
                     AND m.FechaReg < @Manana
               ), 0) AS CobradoHoy,
               ISNULL((
                   SELECT SUM(m.ImportePago)
                   FROM CREDITO.MovimientoCaja AS m
                   INNER JOIN #Creditos AS cr ON cr.CreditoId = m.CreditoId
                   WHERE m.Operacion = 'CUO'
                     AND m.Estado = 1
                     AND m.ImportePago > 0
                     AND m.FechaReg >= @Ayer
                     AND m.FechaReg < @Hoy
               ), 0) AS CobradoAyer,
               ISNULL((SELECT SUM(mp.SaldoTotal) FROM #MoraPersona AS mp), 0) AS SaldoActual,
               ISNULL((SELECT SUM(mp.SaldoMora) FROM #MoraPersona AS mp), 0) AS MontoMora,
               ISNULL((SELECT SUM(CASE WHEN mp.TieneMora = 1 THEN 1 ELSE 0 END) FROM #MoraPersona AS mp), 0) AS ClientesMora,
               ISNULL((SELECT SUM(CASE WHEN mp.TieneMora = 1 AND mp.PagoDesdeInicioMora = 0 THEN 1 ELSE 0 END) FROM #MoraPersona AS mp), 0) AS ClientesMoraSinPago,
               ISNULL((SELECT SUM(CASE WHEN mp.TieneMora = 1 AND mp.PagadoEnCreditosMora <= 0 THEN 1 ELSE 0 END) FROM #MoraPersona AS mp), 0) AS ClientesMoraNuncaPagaron,
               ISNULL((SELECT SUM(CASE WHEN mp.TieneMora = 1 AND mp.PagadoEnCreditosMora > 0 AND mp.PagoDesdeInicioMora = 0 THEN 1 ELSE 0 END) FROM #MoraPersona AS mp), 0) AS ClientesMoraDejaronPagar,
               ISNULL((SELECT SUM(CASE WHEN mp.TieneMora = 1 AND mp.PagoDesdeInicioMora = 1 THEN 1 ELSE 0 END) FROM #MoraPersona AS mp), 0) AS ClientesMoraPagandoConAtraso,
               (
                   SELECT COUNT(*)
                   FROM #Creditos AS cr
                   WHERE cr.Estado = 'DES'
                     AND cr.IndIrrecuperable = 0
                     AND cr.FechaVencimiento >= @Hoy
                     AND cr.FechaVencimiento < @LimiteVencer
               ) AS PorVencerSemana;

        ;WITH Dias AS (
            SELECT DATEADD(DAY, n.n, @InicioProd) AS Fecha
            FROM (VALUES
                (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),
                (10),(11),(12),(13),(14),(15),(16),(17),(18),(19),
                (20),(21),(22),(23),(24),(25),(26),(27),(28),(29)
            ) AS n(n)
        ),
        CobradoDia AS (
            SELECT CAST(m.FechaReg AS date) AS Fecha,
                   SUM(m.ImportePago) AS MontoCobrado
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN #Creditos AS cr ON cr.CreditoId = m.CreditoId
            WHERE m.Operacion = 'CUO'
              AND m.Estado = 1
              AND m.ImportePago > 0
              AND m.FechaReg >= @InicioProd
              AND m.FechaReg < @Manana
            GROUP BY CAST(m.FechaReg AS date)
        )
        SELECT CAST(d.Fecha AS datetime) AS Fecha,
               CONVERT(char(5), d.Fecha, 103) AS Etiqueta,
               ISNULL(c.MontoCobrado, 0) AS MontoCobrado
        FROM Dias AS d
        LEFT JOIN CobradoDia AS c ON c.Fecha = d.Fecha
        ORDER BY d.Fecha;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<DashboardAnalistaDto> ObtenerAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        EnsureConnection();

        var cacheKey = $"dashboard:analista:v2:{usuarioId}:{oficinaId}";
        if (cache.TryGetValue(cacheKey, out DashboardAnalistaDto? cached) && cached is not null)
        {
            return cached;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                Sql,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var kpisRow = await multi.ReadSingleAsync<KpisRow>().ConfigureAwait(false);
        var productividad = (await multi.ReadAsync<DashboardProductividadPuntoDto>().ConfigureAwait(false)).AsList();

        var kpis = DashboardAnalistaInsights.ConVariaciones(
            kpisRow.TotalClientes,
            kpisRow.ClientesNuevosActual,
            kpisRow.ClientesNuevosAnterior,
            kpisRow.CreditosActual,
            kpisRow.CreditosAnterior,
            kpisRow.CobradoActual,
            kpisRow.CobradoAnterior,
            kpisRow.CobradoHoy,
            kpisRow.CobradoAyer,
            kpisRow.SaldoActual,
            kpisRow.MontoMora,
            kpisRow.ClientesMora,
            kpisRow.ClientesMoraSinPago,
            kpisRow.ClientesMoraNuncaPagaron,
            kpisRow.ClientesMoraDejaronPagar,
            kpisRow.ClientesMoraPagandoConAtraso,
            kpisRow.PorVencerSemana);

        var dto = new DashboardAnalistaDto(
            NombreAnalista: string.IsNullOrWhiteSpace(kpisRow.NombreAnalista)
                ? "Analista"
                : kpisRow.NombreAnalista.Trim(),
            FechaConsulta: kpisRow.FechaConsulta,
            Kpis: kpis,
            Productividad: productividad,
            Ranking: Array.Empty<DashboardRankingRowDto>(),
            PodioMesAnterior: Array.Empty<DashboardPodioRowDto>(),
            Insights: DashboardAnalistaInsights.Build(kpis));

        cache.Set(cacheKey, dto, CacheTtl);
        return dto;
    }

    public async Task<IReadOnlyList<DashboardClienteMoraRowDto>> ObtenerClientesMoraAsync(
        int usuarioId,
        int oficinaId,
        string tipo,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        var tipoNorm = DashboardMoraTipos.Normalizar(tipo);
        EnsureConnection();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<ClienteMoraSpRow>(
            new CommandDefinition(
                "CREDITO.usp_DashboardGestorClientesMora",
                new
                {
                    UsuarioId = usuarioId,
                    OficinaId = oficinaId,
                    Tipo = tipoNorm,
                    FechaCorte = (DateTime?)null,
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: CommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows
            .Select(r => new DashboardClienteMoraRowDto(
                PersonaId: r.PersonaId ?? 0,
                NombreCompleto: string.IsNullOrWhiteSpace(r.NombreCompleto)
                    ? "Sin nombre"
                    : r.NombreCompleto.Trim(),
                CreditosMora: r.CreditosMora ?? 0,
                SaldoMora: r.SaldoMora ?? 0m,
                PrimeraCuotaVencida: r.PrimeraCuotaVencida,
                FechaUltimoPago: r.FechaUltimoPago,
                DiasAtraso: r.DiasAtraso ?? 0,
                CodigoClasificacion: r.CodigoClasificacion ?? string.Empty,
                Clasificacion: r.Clasificacion ?? string.Empty))
            .Where(r => r.PersonaId > 0)
            .ToList();
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class KpisRow
    {
        public string NombreAnalista { get; init; } = "Analista";
        public DateTime FechaConsulta { get; init; }
        public int TotalClientes { get; init; }
        public int ClientesNuevosActual { get; init; }
        public int ClientesNuevosAnterior { get; init; }
        public int CreditosActual { get; init; }
        public int CreditosAnterior { get; init; }
        public decimal CobradoActual { get; init; }
        public decimal CobradoAnterior { get; init; }
        public decimal CobradoHoy { get; init; }
        public decimal CobradoAyer { get; init; }
        public decimal SaldoActual { get; init; }
        public decimal MontoMora { get; init; }
        public int ClientesMora { get; init; }
        public int ClientesMoraSinPago { get; init; }
        public int ClientesMoraNuncaPagaron { get; init; }
        public int ClientesMoraDejaronPagar { get; init; }
        public int ClientesMoraPagandoConAtraso { get; init; }
        public int PorVencerSemana { get; init; }
    }

    private sealed class ClienteMoraSpRow
    {
        public int? PersonaId { get; init; }
        public string? NombreCompleto { get; init; }
        public int? CreditosMora { get; init; }
        public decimal? SaldoMora { get; init; }
        public DateTime? PrimeraCuotaVencida { get; init; }
        public DateTime? FechaUltimoPago { get; init; }
        public int? DiasAtraso { get; init; }
        public string? CodigoClasificacion { get; init; }
        public string? Clasificacion { get; init; }
    }
}
