using Credito.Modern.Application.Dashboard;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Dashboard;

/// <summary>
/// Tablero del analista en un solo roundtrip. Replica la semántica de
/// <c>usp_DashboardGestor</c>, <c>usp_DashboardProductividad</c>,
/// <c>usp_DashboardRanking</c> y <c>usp_DashboardTopAnterior</c> sin depender
/// de esos SP (no versionados y con timeouts en producción).
/// </summary>
public sealed class DashboardAnalistaReadService(IOptions<SqlDatabaseOptions> options)
    : IDashboardAnalistaReadService
{
    private const int CommandTimeoutSeconds = 60;

    private static readonly string Sql = """
        DECLARE @Hoy date = dbo.ufnFecha();
        DECLARE @Manana date = DATEADD(DAY, 1, @Hoy);
        DECLARE @InicioMes date = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);
        DECLARE @InicioMesAnterior date = DATEADD(MONTH, -1, @InicioMes);
        DECLARE @InicioProd date = DATEADD(DAY, -29, @Hoy);
        DECLARE @LimiteVencer date = DATEADD(DAY, 8, @Hoy);

        IF OBJECT_ID('tempdb..#Creditos') IS NOT NULL DROP TABLE #Creditos;
        IF OBJECT_ID('tempdb..#Saldos') IS NOT NULL DROP TABLE #Saldos;

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

        CREATE TABLE #Saldos (
            CreditoId int NOT NULL PRIMARY KEY,
            PersonaId int NOT NULL,
            Saldo decimal(16, 2) NOT NULL
        );

        INSERT INTO #Saldos (CreditoId, PersonaId, Saldo)
        SELECT cr.CreditoId,
               cr.PersonaId,
               CASE
                   WHEN ISNULL(prog.Programado, 0) - ISNULL(pag.Pagado, 0) > 0
                       THEN ISNULL(prog.Programado, 0) - ISNULL(pag.Pagado, 0)
                   ELSE 0
               END
        FROM #Creditos AS cr
        LEFT JOIN (
            SELECT pp.CreditoId,
                   SUM(pp.Cuota + pp.Cargo) AS Programado
            FROM CREDITO.PlanPago AS pp
            INNER JOIN #Creditos AS cr2 ON cr2.CreditoId = pp.CreditoId
            WHERE cr2.Estado = 'DES'
              AND cr2.IndIrrecuperable = 0
            GROUP BY pp.CreditoId
        ) AS prog ON prog.CreditoId = cr.CreditoId
        LEFT JOIN (
            SELECT m.CreditoId,
                   SUM(m.ImportePago) AS Pagado
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN #Creditos AS cr3 ON cr3.CreditoId = m.CreditoId
            WHERE cr3.Estado = 'DES'
              AND cr3.IndIrrecuperable = 0
              AND m.Operacion = 'CUO'
              AND m.Estado = 1
              AND m.ImportePago > 0
            GROUP BY m.CreditoId
        ) AS pag ON pag.CreditoId = cr.CreditoId
        WHERE cr.Estado = 'DES'
          AND cr.IndIrrecuperable = 0;

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
               ISNULL((SELECT SUM(s.Saldo) FROM #Saldos AS s), 0) AS SaldoActual,
               (
                   SELECT COUNT(DISTINCT s.PersonaId)
                   FROM #Saldos AS s
                   INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = s.CreditoId
                   WHERE s.Saldo > 0
                     AND pp.Estado = 'PEN'
                     AND pp.FechaVencimiento < @Hoy
               ) AS ClientesMora,
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

        ;WITH Analistas AS (
            SELECT DISTINCT
                   u.UsuarioId,
                   ISNULL(p.NombreCompleto, u.NombreUsuario) AS NombreCompleto
            FROM MAESTRO.UsuarioRol AS ur
            INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
            LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE ur.OficinaId = @OficinaId
              AND r.Denominacion = N'ANALISTA'
              AND r.Estado = CAST(1 AS bit)
              AND u.Estado = CAST(1 AS bit)
              AND u.NombreUsuario <> N'IRRECUPERABLE'
        ),
        CobranzaMes AS (
            SELECT c.UsuarioRegId AS UsuarioId,
                   SUM(m.ImportePago) AS TotalCobrado
            FROM CREDITO.Credito AS c
            INNER JOIN CREDITO.MovimientoCaja AS m
                ON m.CreditoId = c.CreditoId
               AND m.Operacion = 'CUO'
               AND m.Estado = 1
               AND m.ImportePago > 0
               AND m.FechaReg >= @InicioMes
               AND m.FechaReg < @Manana
            WHERE c.OficinaId = @OficinaId
            GROUP BY c.UsuarioRegId
        )
        SELECT a.UsuarioId,
               a.NombreCompleto,
               CAST(ISNULL(c.TotalCobrado, 0) AS decimal(16, 2)) AS TotalCobrado,
               CAST(DENSE_RANK() OVER (ORDER BY ISNULL(c.TotalCobrado, 0) DESC) AS int) AS Posicion,
               CAST(CASE WHEN a.UsuarioId = @UsuarioId THEN 1 ELSE 0 END AS bit) AS EsUsuarioActual
        FROM Analistas AS a
        LEFT JOIN CobranzaMes AS c ON c.UsuarioId = a.UsuarioId
        ORDER BY Posicion, a.NombreCompleto;

        ;WITH Analistas AS (
            SELECT DISTINCT
                   u.UsuarioId,
                   ISNULL(p.NombreCompleto, u.NombreUsuario) AS NombreCompleto
            FROM MAESTRO.UsuarioRol AS ur
            INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
            LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE ur.OficinaId = @OficinaId
              AND r.Denominacion = N'ANALISTA'
              AND r.Estado = CAST(1 AS bit)
              AND u.Estado = CAST(1 AS bit)
              AND u.NombreUsuario <> N'IRRECUPERABLE'
        ),
        CobranzaAnterior AS (
            SELECT c.UsuarioRegId AS UsuarioId,
                   SUM(m.ImportePago) AS TotalCobrado
            FROM CREDITO.Credito AS c
            INNER JOIN CREDITO.MovimientoCaja AS m
                ON m.CreditoId = c.CreditoId
               AND m.Operacion = 'CUO'
               AND m.Estado = 1
               AND m.ImportePago > 0
               AND m.FechaReg >= @InicioMesAnterior
               AND m.FechaReg < @InicioMes
            WHERE c.OficinaId = @OficinaId
            GROUP BY c.UsuarioRegId
        ),
        Ranked AS (
            SELECT a.UsuarioId,
                   a.NombreCompleto,
                   CAST(ISNULL(c.TotalCobrado, 0) AS decimal(16, 2)) AS TotalCobrado,
                   CAST(ROW_NUMBER() OVER (
                       ORDER BY ISNULL(c.TotalCobrado, 0) DESC, a.NombreCompleto) AS int) AS Posicion
            FROM Analistas AS a
            LEFT JOIN CobranzaAnterior AS c ON c.UsuarioId = a.UsuarioId
        )
        SELECT TOP (3)
               r.UsuarioId,
               r.NombreCompleto,
               r.TotalCobrado,
               r.Posicion
        FROM Ranked AS r
        WHERE r.TotalCobrado > 0
        ORDER BY r.Posicion;
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
        var ranking = (await multi.ReadAsync<DashboardRankingRowDto>().ConfigureAwait(false)).AsList();
        var podio = (await multi.ReadAsync<DashboardPodioRowDto>().ConfigureAwait(false)).AsList();

        var kpis = DashboardAnalistaInsights.ConVariaciones(
            kpisRow.TotalClientes,
            kpisRow.ClientesNuevosActual,
            kpisRow.ClientesNuevosAnterior,
            kpisRow.CreditosActual,
            kpisRow.CreditosAnterior,
            kpisRow.CobradoActual,
            kpisRow.CobradoAnterior,
            kpisRow.SaldoActual,
            kpisRow.ClientesMora,
            kpisRow.PorVencerSemana);

        return new DashboardAnalistaDto(
            NombreAnalista: string.IsNullOrWhiteSpace(kpisRow.NombreAnalista)
                ? "Analista"
                : kpisRow.NombreAnalista.Trim(),
            FechaConsulta: kpisRow.FechaConsulta,
            Kpis: kpis,
            Productividad: productividad,
            Ranking: ranking,
            PodioMesAnterior: podio,
            Insights: DashboardAnalistaInsights.Build(kpis));
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
        public decimal SaldoActual { get; init; }
        public int ClientesMora { get; init; }
        public int PorVencerSemana { get; init; }
    }
}
