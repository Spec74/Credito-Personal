using Credito.Modern.Application.Dashboard;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Dashboard;

/// <summary>
/// Tablero gerencial optimizado. Replica la semántica de salida de
/// <c>usp_DashboardAdmin*</c> acotada a <c>OficinaId</c> del JWT.
/// Shell: KPIs sin PlanPago. Detalle: cartera vía #Saldos + flujo/históricos/analistas.
/// </summary>
public sealed class DashboardAdminReadService(
    IOptions<SqlDatabaseOptions> options,
    IMemoryCache cache) : IDashboardAdminReadService
{
    private const int ShellCommandTimeoutSeconds = 20;
    private const int DetalleCommandTimeoutSeconds = 60;
    private static readonly TimeSpan ShellCacheTtl = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan DetalleCacheTtl = TimeSpan.FromSeconds(180);
    private static readonly TimeSpan CombinedCacheTtl = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Shell sin PlanPago ni #temp: Coloc/Cob/Flujo/Vencer + conteo barato de clientes.
    /// Saldos de cartera llegan en cero; detalle los completa vía #Saldos.
    /// </summary>
    private static readonly string SqlShell = """
        DECLARE @Hoy date = dbo.ufnFecha();
        DECLARE @Ayer date = DATEADD(DAY, -1, @Hoy);
        DECLARE @Anteayer date = DATEADD(DAY, -2, @Hoy);
        DECLARE @Manana date = DATEADD(DAY, 1, @Hoy);
        DECLARE @InicioMes date = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);
        DECLARE @InicioMesAnterior date = DATEADD(MONTH, -1, @InicioMes);
        DECLARE @DiasTranscurridos int = DATEDIFF(DAY, @InicioMes, @Manana);
        DECLARE @FinComparableAnterior date = DATEADD(DAY, @DiasTranscurridos, @InicioMesAnterior);
        DECLARE @LimiteVencer date = DATEADD(DAY, 8, @Hoy);

        ;WITH Coloc AS (
            SELECT
                SUM(CASE WHEN c.FechaDesembolso >= @Hoy AND c.FechaDesembolso < @Manana THEN 1 ELSE 0 END) AS CreditosHoy,
                SUM(CASE WHEN c.FechaDesembolso >= @Ayer AND c.FechaDesembolso < @Hoy THEN 1 ELSE 0 END) AS CreditosAyer,
                SUM(CASE WHEN c.FechaDesembolso >= @Anteayer AND c.FechaDesembolso < @Ayer THEN 1 ELSE 0 END) AS CreditosAnteayer,
                SUM(CASE WHEN c.FechaDesembolso >= @InicioMes AND c.FechaDesembolso < @Manana THEN 1 ELSE 0 END) AS CreditosMesActual,
                SUM(CASE WHEN c.FechaDesembolso >= @InicioMesAnterior AND c.FechaDesembolso < @FinComparableAnterior THEN 1 ELSE 0 END) AS CreditosMesAnteriorComparable,
                SUM(CASE WHEN c.FechaDesembolso >= @Hoy AND c.FechaDesembolso < @Manana THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoHoy,
                SUM(CASE WHEN c.FechaDesembolso >= @Ayer AND c.FechaDesembolso < @Hoy THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoAyer,
                SUM(CASE WHEN c.FechaDesembolso >= @Anteayer AND c.FechaDesembolso < @Ayer THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoAnteayer,
                SUM(CASE WHEN c.FechaDesembolso >= @InicioMes AND c.FechaDesembolso < @Manana THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoMesActual,
                SUM(CASE WHEN c.FechaDesembolso >= @InicioMesAnterior AND c.FechaDesembolso < @FinComparableAnterior THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoMesAnteriorComparable
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.FechaDesembolso >= @InicioMesAnterior
              AND c.FechaDesembolso < @Manana
              AND c.Estado IN ('DES', 'PAG', 'REP')
        ),
        Vencer AS (
            SELECT COUNT(*) AS CreditosPorVencerSemana
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.Estado = 'DES'
              AND c.IndIrrecuperable = 0
              AND c.FechaVencimiento >= @Hoy
              AND c.FechaVencimiento < @LimiteVencer
        ),
        Cob AS (
            SELECT
                SUM(CASE WHEN m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS CobradoHoy,
                SUM(CASE WHEN m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END) AS CobradoAyer,
                SUM(CASE WHEN m.FechaReg >= @Anteayer AND m.FechaReg < @Ayer THEN m.ImportePago ELSE 0 END) AS CobradoAnteayer,
                SUM(CASE WHEN m.FechaReg >= @InicioMes AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS CobradoMesActual,
                SUM(CASE WHEN m.FechaReg >= @InicioMesAnterior AND m.FechaReg < @FinComparableAnterior THEN m.ImportePago ELSE 0 END) AS CobradoMesAnteriorComparable
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.Credito AS c ON c.CreditoId = m.CreditoId
            WHERE c.OficinaId = @OficinaId
              AND m.Operacion = 'CUO'
              AND m.Estado = 1
              AND m.ImportePago > 0
              AND m.FechaReg >= @InicioMesAnterior
              AND m.FechaReg < @Manana
        ),
        Flujo AS (
            SELECT
                SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS EntradasHoy,
                SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS SalidasHoy,
                SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END) AS EntradasAyer,
                SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END) AS SalidasAyer,
                SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @Anteayer AND m.FechaReg < @Ayer THEN m.ImportePago ELSE 0 END) AS EntradasAnteayer,
                SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @Anteayer AND m.FechaReg < @Ayer THEN m.ImportePago ELSE 0 END) AS SalidasAnteayer,
                SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @InicioMes AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS EntradasMesActual,
                SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @InicioMes AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS SalidasMesActual,
                SUM(CASE WHEN m.IndEntrada = 1 AND m.FechaReg >= @InicioMesAnterior AND m.FechaReg < @FinComparableAnterior THEN m.ImportePago ELSE 0 END) AS EntradasMesAnteriorComparable,
                SUM(CASE WHEN m.IndEntrada = 0 AND m.FechaReg >= @InicioMesAnterior AND m.FechaReg < @FinComparableAnterior THEN m.ImportePago ELSE 0 END) AS SalidasMesAnteriorComparable
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = m.CajaDiarioId
            INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
            WHERE ca.OficinaId = @OficinaId
              AND m.Estado = 1
              AND m.FechaReg >= @InicioMesAnterior
              AND m.FechaReg < @Manana
        ),
        Clientes AS (
            SELECT COUNT(DISTINCT c.PersonaId) AS TotalClientes
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.Estado = 'DES'
              AND c.IndIrrecuperable = 0
              AND c.FechaDesembolso IS NOT NULL
        )
        SELECT ISNULL((
                   SELECT o.Denominacion FROM MAESTRO.Oficina AS o WHERE o.OficinaId = @OficinaId
               ), N'Oficina') AS NombreOficina,
               CAST(@Hoy AS datetime) AS FechaConsulta,
               (
                   SELECT COUNT(DISTINCT u.UsuarioId)
                   FROM MAESTRO.UsuarioRol AS ur
                   INNER JOIN MAESTRO.Rol AS r ON r.RolId = ur.RolId
                   INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
                   WHERE ur.OficinaId = @OficinaId
                     AND r.Denominacion = N'ANALISTA'
                     AND r.Estado = CAST(1 AS bit)
                     AND u.Estado = CAST(1 AS bit)
                     AND u.NombreUsuario <> N'IRRECUPERABLE'
               ) AS TotalAnalistas,
               ISNULL((SELECT TotalClientes FROM Clientes), 0) AS TotalClientes,
               ISNULL((SELECT CreditosHoy FROM Coloc), 0) AS CreditosHoy,
               ISNULL((SELECT CreditosAyer FROM Coloc), 0) AS CreditosAyer,
               ISNULL((SELECT CreditosAnteayer FROM Coloc), 0) AS CreditosAnteayer,
               ISNULL((SELECT CreditosMesActual FROM Coloc), 0) AS CreditosMesActual,
               ISNULL((SELECT CreditosMesAnteriorComparable FROM Coloc), 0) AS CreditosMesAnteriorComparable,
               ISNULL((SELECT DesembolsoHoy FROM Coloc), 0) AS DesembolsoHoy,
               ISNULL((SELECT DesembolsoAyer FROM Coloc), 0) AS DesembolsoAyer,
               ISNULL((SELECT DesembolsoAnteayer FROM Coloc), 0) AS DesembolsoAnteayer,
               ISNULL((SELECT DesembolsoMesActual FROM Coloc), 0) AS DesembolsoMesActual,
               ISNULL((SELECT DesembolsoMesAnteriorComparable FROM Coloc), 0) AS DesembolsoMesAnteriorComparable,
               ISNULL((SELECT CobradoHoy FROM Cob), 0) AS CobradoHoy,
               ISNULL((SELECT CobradoAyer FROM Cob), 0) AS CobradoAyer,
               ISNULL((SELECT CobradoAnteayer FROM Cob), 0) AS CobradoAnteayer,
               ISNULL((SELECT CobradoMesActual FROM Cob), 0) AS CobradoMesActual,
               ISNULL((SELECT CobradoMesAnteriorComparable FROM Cob), 0) AS CobradoMesAnteriorComparable,
               ISNULL((SELECT EntradasHoy FROM Flujo), 0) AS EntradasHoy,
               ISNULL((SELECT SalidasHoy FROM Flujo), 0) AS SalidasHoy,
               ISNULL((SELECT EntradasAyer FROM Flujo), 0) AS EntradasAyer,
               ISNULL((SELECT SalidasAyer FROM Flujo), 0) AS SalidasAyer,
               ISNULL((SELECT EntradasAnteayer FROM Flujo), 0) AS EntradasAnteayer,
               ISNULL((SELECT SalidasAnteayer FROM Flujo), 0) AS SalidasAnteayer,
               ISNULL((SELECT EntradasMesActual FROM Flujo), 0) AS EntradasMesActual,
               ISNULL((SELECT SalidasMesActual FROM Flujo), 0) AS SalidasMesActual,
               ISNULL((SELECT EntradasMesAnteriorComparable FROM Flujo), 0) AS EntradasMesAnteriorComparable,
               ISNULL((SELECT SalidasMesAnteriorComparable FROM Flujo), 0) AS SalidasMesAnteriorComparable,
               CAST(0 AS decimal(16, 2)) AS SaldoCartera,
               CAST(0 AS decimal(16, 2)) AS SaldoCreditos,
               CAST(0 AS decimal(16, 2)) AS SaldoMoraCartera,
               CAST(0 AS decimal(16, 2)) AS SaldoVencido,
               CAST(0 AS decimal(16, 2)) AS SaldoMorosidad,
               0 AS ClientesMora,
               ISNULL((SELECT CreditosPorVencerSemana FROM Vencer), 0) AS CreditosPorVencerSemana;
        """;

    private const string SqlPreamble = """
        DECLARE @Hoy date = dbo.ufnFecha();
        DECLARE @Ayer date = DATEADD(DAY, -1, @Hoy);
        DECLARE @Anteayer date = DATEADD(DAY, -2, @Hoy);
        DECLARE @Manana date = DATEADD(DAY, 1, @Hoy);
        DECLARE @InicioMes date = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);
        DECLARE @InicioMesAnterior date = DATEADD(MONTH, -1, @InicioMes);
        DECLARE @DiasTranscurridos int = DATEDIFF(DAY, @InicioMes, @Manana);
        DECLARE @FinComparableAnterior date = DATEADD(DAY, @DiasTranscurridos, @InicioMesAnterior);
        DECLARE @InicioHist date = DATEADD(DAY, -29, @Hoy);
        DECLARE @InicioMensual date = DATEADD(MONTH, -11, @InicioMes);
        DECLARE @LimiteVencer date = DATEADD(DAY, 8, @Hoy);

        IF OBJECT_ID('tempdb..#Creditos') IS NOT NULL DROP TABLE #Creditos;
        IF OBJECT_ID('tempdb..#Saldos') IS NOT NULL DROP TABLE #Saldos;

        CREATE TABLE #Creditos (
            CreditoId int NOT NULL PRIMARY KEY,
            PersonaId int NOT NULL,
            UsuarioRegId int NOT NULL,
            Estado char(3) NOT NULL,
            IndIrrecuperable bit NOT NULL,
            FechaDesembolso datetime NULL,
            FechaVencimiento date NOT NULL,
            MontoDesembolso decimal(16, 2) NOT NULL
        );

        INSERT INTO #Creditos (
            CreditoId, PersonaId, UsuarioRegId, Estado, IndIrrecuperable,
            FechaDesembolso, FechaVencimiento, MontoDesembolso)
        SELECT c.CreditoId,
               c.PersonaId,
               c.UsuarioRegId,
               c.Estado,
               c.IndIrrecuperable,
               c.FechaDesembolso,
               c.FechaVencimiento,
               c.MontoDesembolso
        FROM CREDITO.Credito AS c
        WHERE c.OficinaId = @OficinaId
          AND c.Estado = 'DES'
          AND c.IndIrrecuperable = 0
          AND c.FechaDesembolso IS NOT NULL
          AND c.FechaDesembolso < @Manana;

        CREATE TABLE #Saldos (
            CreditoId int NOT NULL PRIMARY KEY,
            PersonaId int NOT NULL,
            UsuarioRegId int NOT NULL,
            FechaVencimiento date NOT NULL,
            Saldo decimal(16, 2) NOT NULL,
            EnMora bit NOT NULL,
            Morosidad decimal(16, 2) NOT NULL
        );

        INSERT INTO #Saldos (CreditoId, PersonaId, UsuarioRegId, FechaVencimiento, Saldo, EnMora, Morosidad)
        SELECT cr.CreditoId,
               cr.PersonaId,
               cr.UsuarioRegId,
               cr.FechaVencimiento,
               ISNULL(pen.Saldo, 0),
               CAST(ISNULL(pen.EnMora, 0) AS bit),
               ISNULL(pen.Morosidad, 0)
        FROM #Creditos AS cr
        LEFT JOIN (
            SELECT pp.CreditoId,
                   SUM(CASE
                       WHEN pp.Cuota + pp.Cargo - ISNULL(pp.PagoCuota, 0) - pp.PagoLibre > 0
                           THEN pp.Cuota + pp.Cargo - ISNULL(pp.PagoCuota, 0) - pp.PagoLibre
                       ELSE 0
                   END) AS Saldo,
                   MAX(CASE WHEN pp.FechaVencimiento < @Hoy THEN 1 ELSE 0 END) AS EnMora,
                   SUM(CASE
                       WHEN pp.FechaVencimiento < @Hoy
                            AND pp.Cuota + pp.Cargo - ISNULL(pp.PagoCuota, 0) - pp.PagoLibre > 0
                       THEN pp.Cuota + pp.Cargo - ISNULL(pp.PagoCuota, 0) - pp.PagoLibre
                       ELSE 0 END) AS Morosidad
            FROM CREDITO.PlanPago AS pp
            INNER JOIN #Creditos AS cr2 ON cr2.CreditoId = pp.CreditoId
            WHERE pp.Estado = 'PEN'
            GROUP BY pp.CreditoId
        ) AS pen ON pen.CreditoId = cr.CreditoId;
        """;

    private static readonly string SqlDetalle = SqlPreamble + """

        SELECT
          COUNT(DISTINCT s.PersonaId) AS TotalClientes,
          ISNULL(SUM(s.Saldo), 0) AS SaldoCartera,
          ISNULL(SUM(CASE WHEN s.EnMora = 0 THEN s.Saldo ELSE 0 END), 0) AS SaldoCreditos,
          ISNULL(SUM(CASE WHEN s.EnMora = 1 THEN s.Saldo ELSE 0 END), 0) AS SaldoMoraCartera,
          ISNULL(SUM(CASE WHEN s.FechaVencimiento < @Hoy THEN s.Saldo ELSE 0 END), 0) AS SaldoVencido,
          ISNULL(SUM(s.Morosidad), 0) AS SaldoMorosidad,
          COUNT(DISTINCT CASE WHEN s.Saldo > 0 AND s.EnMora = 1 THEN s.PersonaId END) AS ClientesMora
        FROM #Saldos AS s;

        SELECT m.Operacion,
               m.IndEntrada,
               ISNULL(t.Denominacion, m.Operacion) AS Concepto,
               CAST(CASE
                   WHEN t.Denominacion LIKE N'%TRANSFERENCIA%'
                     OR m.Operacion IN ('TRB', 'TRA', 'TBO', 'TCH', 'TRS')
                   THEN 1 ELSE 0 END AS bit) AS EsTransferencia,
               SUM(CASE WHEN m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN 1 ELSE 0 END) AS CantidadHoy,
               SUM(CASE WHEN m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS ImporteHoy,
               SUM(CASE WHEN m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN 1 ELSE 0 END) AS CantidadAyer,
               SUM(CASE WHEN m.FechaReg >= @Ayer AND m.FechaReg < @Hoy THEN m.ImportePago ELSE 0 END) AS ImporteAyer,
               SUM(CASE WHEN m.FechaReg >= @InicioMes AND m.FechaReg < @Manana THEN 1 ELSE 0 END) AS CantidadMesActual,
               SUM(CASE WHEN m.FechaReg >= @InicioMes AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS ImporteMesActual,
               SUM(CASE WHEN m.FechaReg >= @InicioMesAnterior AND m.FechaReg < @FinComparableAnterior THEN 1 ELSE 0 END) AS CantidadMesAnteriorComparable,
               SUM(CASE WHEN m.FechaReg >= @InicioMesAnterior AND m.FechaReg < @FinComparableAnterior THEN m.ImportePago ELSE 0 END) AS ImporteMesAnteriorComparable
        FROM CREDITO.MovimientoCaja AS m
        INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = m.CajaDiarioId
        INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
        LEFT JOIN MAESTRO.TipoOperacion AS t ON t.Codigo = m.Operacion
        WHERE ca.OficinaId = @OficinaId
          AND m.Estado = 1
          AND m.FechaReg >= @InicioMesAnterior
          AND m.FechaReg < @Manana
        GROUP BY m.Operacion, m.IndEntrada, t.Denominacion
        HAVING SUM(m.ImportePago) <> 0
        ORDER BY m.IndEntrada DESC, ISNULL(t.Denominacion, m.Operacion);

        ;WITH Dias AS (
            SELECT DATEADD(DAY, n.n, @InicioHist) AS Fecha
            FROM (VALUES
                (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),
                (10),(11),(12),(13),(14),(15),(16),(17),(18),(19),
                (20),(21),(22),(23),(24),(25),(26),(27),(28),(29)
            ) AS n(n)
        ),
        ColocDia AS (
            SELECT CAST(c.FechaDesembolso AS date) AS Fecha,
                   COUNT(*) AS Colocaciones,
                   SUM(c.MontoDesembolso) AS Desembolsado
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.Estado IN ('DES', 'PAG', 'REP')
              AND c.FechaDesembolso >= @InicioHist
              AND c.FechaDesembolso < @Manana
            GROUP BY CAST(c.FechaDesembolso AS date)
        ),
        CobradoDia AS (
            SELECT CAST(m.FechaReg AS date) AS Fecha,
                   SUM(m.ImportePago) AS Cobrado
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.Credito AS c ON c.CreditoId = m.CreditoId
            WHERE c.OficinaId = @OficinaId
              AND m.Operacion = 'CUO' AND m.Estado = 1 AND m.ImportePago > 0
              AND m.FechaReg >= @InicioHist AND m.FechaReg < @Manana
            GROUP BY CAST(m.FechaReg AS date)
        ),
        FlujoDia AS (
            SELECT CAST(m.FechaReg AS date) AS Fecha,
                   SUM(CASE WHEN m.IndEntrada = 1 THEN m.ImportePago ELSE 0 END) AS Entradas,
                   SUM(CASE WHEN m.IndEntrada = 0 THEN m.ImportePago ELSE 0 END) AS Salidas,
                   SUM(CASE
                       WHEN t.Denominacion LIKE N'%TRANSFERENCIA%'
                         OR m.Operacion IN ('TRB', 'TRA', 'TBO', 'TCH', 'TRS')
                       THEN 0
                       WHEN m.IndEntrada = 1 THEN m.ImportePago
                       ELSE -m.ImportePago
                   END) AS FlujoOperativo
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = m.CajaDiarioId
            INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
            LEFT JOIN MAESTRO.TipoOperacion AS t ON t.Codigo = m.Operacion
            WHERE ca.OficinaId = @OficinaId AND m.Estado = 1
              AND m.FechaReg >= @InicioHist AND m.FechaReg < @Manana
            GROUP BY CAST(m.FechaReg AS date)
        )
        SELECT CAST(d.Fecha AS datetime) AS Fecha,
               CONVERT(char(5), d.Fecha, 103) AS Etiqueta,
               ISNULL(c.Colocaciones, 0) AS Colocaciones,
               ISNULL(c.Desembolsado, 0) AS Desembolsado,
               ISNULL(b.Cobrado, 0) AS Cobrado,
               ISNULL(f.Entradas, 0) AS Entradas,
               ISNULL(f.Salidas, 0) AS Salidas,
               ISNULL(f.Entradas, 0) - ISNULL(f.Salidas, 0) AS FlujoNeto,
               ISNULL(f.FlujoOperativo, 0) AS FlujoOperativo
        FROM Dias AS d
        LEFT JOIN ColocDia AS c ON c.Fecha = d.Fecha
        LEFT JOIN CobradoDia AS b ON b.Fecha = d.Fecha
        LEFT JOIN FlujoDia AS f ON f.Fecha = d.Fecha
        ORDER BY d.Fecha;

        ;WITH Meses AS (
            SELECT DATEADD(MONTH, -n.n, @InicioMes) AS FechaMes
            FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11)) AS n(n)
        ),
        ColocMes AS (
            SELECT DATEFROMPARTS(YEAR(c.FechaDesembolso), MONTH(c.FechaDesembolso), 1) AS FechaMes,
                   COUNT(*) AS Colocaciones,
                   SUM(c.MontoDesembolso) AS Desembolsado
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.Estado IN ('DES', 'PAG', 'REP')
              AND c.FechaDesembolso >= @InicioMensual
              AND c.FechaDesembolso < @Manana
            GROUP BY DATEFROMPARTS(YEAR(c.FechaDesembolso), MONTH(c.FechaDesembolso), 1)
        ),
        CobradoMes AS (
            SELECT DATEFROMPARTS(YEAR(m.FechaReg), MONTH(m.FechaReg), 1) AS FechaMes,
                   SUM(m.ImportePago) AS Cobrado
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.Credito AS c ON c.CreditoId = m.CreditoId
            WHERE c.OficinaId = @OficinaId
              AND m.Operacion = 'CUO' AND m.Estado = 1 AND m.ImportePago > 0
              AND m.FechaReg >= @InicioMensual AND m.FechaReg < @Manana
            GROUP BY DATEFROMPARTS(YEAR(m.FechaReg), MONTH(m.FechaReg), 1)
        ),
        FlujoMes AS (
            SELECT DATEFROMPARTS(YEAR(m.FechaReg), MONTH(m.FechaReg), 1) AS FechaMes,
                   SUM(CASE WHEN m.IndEntrada = 1 THEN m.ImportePago ELSE 0 END) AS Entradas,
                   SUM(CASE WHEN m.IndEntrada = 0 THEN m.ImportePago ELSE 0 END) AS Salidas,
                   SUM(CASE
                       WHEN t.Denominacion LIKE N'%TRANSFERENCIA%'
                         OR m.Operacion IN ('TRB', 'TRA', 'TBO', 'TCH', 'TRS')
                       THEN 0
                       WHEN m.IndEntrada = 1 THEN m.ImportePago
                       ELSE -m.ImportePago
                   END) AS FlujoOperativo
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = m.CajaDiarioId
            INNER JOIN CREDITO.Caja AS ca ON ca.CajaId = cd.CajaId
            LEFT JOIN MAESTRO.TipoOperacion AS t ON t.Codigo = m.Operacion
            WHERE ca.OficinaId = @OficinaId AND m.Estado = 1
              AND m.FechaReg >= @InicioMensual AND m.FechaReg < @Manana
            GROUP BY DATEFROMPARTS(YEAR(m.FechaReg), MONTH(m.FechaReg), 1)
        )
        SELECT CAST(me.FechaMes AS datetime) AS FechaMes,
               CAST(CASE WHEN me.FechaMes = @InicioMes THEN 1 ELSE 0 END AS bit) AS EsMesActual,
               DATENAME(MONTH, me.FechaMes) + ' ' + CAST(YEAR(me.FechaMes) AS varchar(4)) AS Etiqueta,
               ISNULL(c.Colocaciones, 0) AS Colocaciones,
               ISNULL(c.Desembolsado, 0) AS Desembolsado,
               ISNULL(b.Cobrado, 0) AS Cobrado,
               ISNULL(f.Entradas, 0) AS Entradas,
               ISNULL(f.Salidas, 0) AS Salidas,
               ISNULL(f.Entradas, 0) - ISNULL(f.Salidas, 0) AS FlujoNeto,
               ISNULL(f.FlujoOperativo, 0) AS FlujoOperativo
        FROM Meses AS me
        LEFT JOIN ColocMes AS c ON c.FechaMes = me.FechaMes
        LEFT JOIN CobradoMes AS b ON b.FechaMes = me.FechaMes
        LEFT JOIN FlujoMes AS f ON f.FechaMes = me.FechaMes
        ORDER BY me.FechaMes;

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
        SaldosAgg AS (
            SELECT s.UsuarioRegId,
                   COUNT(DISTINCT s.PersonaId) AS TotalClientes,
                   COUNT(DISTINCT CASE WHEN s.Saldo > 0 AND s.EnMora = 1 THEN s.PersonaId END) AS ClientesMora,
                   SUM(CASE WHEN s.EnMora = 1 THEN s.Saldo ELSE 0 END) AS MontoMora
            FROM #Saldos AS s
            GROUP BY s.UsuarioRegId
        ),
        ColocAgg AS (
            SELECT c.UsuarioRegId,
                   SUM(CASE WHEN c.FechaDesembolso >= @Hoy AND c.FechaDesembolso < @Manana THEN 1 ELSE 0 END) AS ColocacionesHoy,
                   SUM(CASE WHEN c.FechaDesembolso >= @InicioMes AND c.FechaDesembolso < @Manana THEN 1 ELSE 0 END) AS ColocacionesMes,
                   SUM(CASE WHEN c.FechaDesembolso >= @Hoy AND c.FechaDesembolso < @Manana THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoHoy,
                   SUM(CASE WHEN c.FechaDesembolso >= @InicioMes AND c.FechaDesembolso < @Manana THEN c.MontoDesembolso ELSE 0 END) AS DesembolsoMes
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.Estado IN ('DES', 'PAG', 'REP')
              AND c.FechaDesembolso IS NOT NULL
              AND c.FechaDesembolso >= @InicioMes
              AND c.FechaDesembolso < @Manana
            GROUP BY c.UsuarioRegId
        ),
        ClientesNuevos AS (
            SELECT c.UsuarioRegId,
                   COUNT(DISTINCT c.PersonaId) AS ClientesNuevosMes
            FROM CREDITO.Credito AS c
            WHERE c.OficinaId = @OficinaId
              AND c.Estado IN ('DES', 'PAG', 'REP')
              AND c.FechaDesembolso >= @InicioMes
              AND c.FechaDesembolso < @Manana
              AND NOT EXISTS (
                  SELECT 1
                  FROM CREDITO.Credito AS prev
                  WHERE prev.OficinaId = @OficinaId
                    AND prev.PersonaId = c.PersonaId
                    AND prev.UsuarioRegId = c.UsuarioRegId
                    AND prev.Estado IN ('DES', 'PAG', 'REP')
                    AND prev.FechaDesembolso IS NOT NULL
                    AND prev.FechaDesembolso < @InicioMes)
            GROUP BY c.UsuarioRegId
        ),
        CobradoAgg AS (
            SELECT c.UsuarioRegId,
                   SUM(CASE WHEN m.FechaReg >= @Hoy AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS CobradoHoy,
                   SUM(CASE WHEN m.FechaReg >= @InicioMes AND m.FechaReg < @Manana THEN m.ImportePago ELSE 0 END) AS CobradoMes,
                   SUM(CASE
                       WHEN m.FechaReg >= @InicioMesAnterior AND m.FechaReg < @FinComparableAnterior
                       THEN m.ImportePago ELSE 0 END) AS CobradoMesAnteriorComparable
            FROM CREDITO.MovimientoCaja AS m
            INNER JOIN CREDITO.Credito AS c ON c.CreditoId = m.CreditoId
            WHERE c.OficinaId = @OficinaId
              AND m.Operacion = 'CUO' AND m.Estado = 1 AND m.ImportePago > 0
              AND m.FechaReg >= @InicioMesAnterior
              AND m.FechaReg < @Manana
            GROUP BY c.UsuarioRegId
        )
        SELECT a.UsuarioId,
               a.NombreCompleto,
               ISNULL(s.TotalClientes, 0) AS TotalClientes,
               ISNULL(n.ClientesNuevosMes, 0) AS ClientesNuevosMes,
               ISNULL(col.ColocacionesHoy, 0) AS ColocacionesHoy,
               ISNULL(col.ColocacionesMes, 0) AS ColocacionesMes,
               ISNULL(col.DesembolsoHoy, 0) AS DesembolsoHoy,
               ISNULL(col.DesembolsoMes, 0) AS DesembolsoMes,
               ISNULL(cob.CobradoHoy, 0) AS CobradoHoy,
               ISNULL(cob.CobradoMes, 0) AS CobradoMes,
               ISNULL(cob.CobradoMesAnteriorComparable, 0) AS CobradoMesAnteriorComparable,
               ISNULL(s.ClientesMora, 0) AS ClientesMora,
               ISNULL(s.MontoMora, 0) AS MontoMora
        FROM Analistas AS a
        LEFT JOIN SaldosAgg AS s ON s.UsuarioRegId = a.UsuarioId
        LEFT JOIN ColocAgg AS col ON col.UsuarioRegId = a.UsuarioId
        LEFT JOIN ClientesNuevos AS n ON n.UsuarioRegId = a.UsuarioId
        LEFT JOIN CobradoAgg AS cob ON cob.UsuarioRegId = a.UsuarioId
        ORDER BY a.NombreCompleto;
        """;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<DashboardAdminDto> ObtenerAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureOficina(oficinaId);
        EnsureConnection();

        var cacheKey = $"dashboard:admin:{oficinaId}";
        if (cache.TryGetValue(cacheKey, out DashboardAdminDto? cached) && cached is not null)
        {
            return cached;
        }

        var shellTask = ObtenerShellAsync(oficinaId, cancellationToken);
        var detalleTask = ObtenerDetalleAsync(oficinaId, cancellationToken);
        await Task.WhenAll(shellTask, detalleTask).ConfigureAwait(false);

        var shell = await shellTask.ConfigureAwait(false);
        var detalle = await detalleTask.ConfigureAwait(false);

        var resumen = MergeCartera(shell.Resumen, detalle.Cartera);
        var dto = new DashboardAdminDto(
            shell.NombreOficina,
            shell.FechaConsulta,
            resumen,
            detalle.FlujoCaja,
            detalle.Historico,
            detalle.HistoricoMensual,
            detalle.Analistas);

        cache.Set(cacheKey, dto, CombinedCacheTtl);
        return dto;
    }

    public async Task<DashboardAdminShellDto> ObtenerShellAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureOficina(oficinaId);
        EnsureConnection();

        var cacheKey = $"dashboard:admin:shell:{oficinaId}";
        if (cache.TryGetValue(cacheKey, out DashboardAdminShellDto? cached) && cached is not null)
        {
            return cached;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var raw = await connection.QuerySingleAsync<ResumenRow>(
            new CommandDefinition(
                SqlShell,
                new { OficinaId = oficinaId },
                commandTimeout: ShellCommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var dto = new DashboardAdminShellDto(
            NombreOficina: string.IsNullOrWhiteSpace(raw.NombreOficina)
                ? "Oficina"
                : raw.NombreOficina.Trim(),
            FechaConsulta: raw.FechaConsulta,
            Resumen: MapResumen(raw));

        cache.Set(cacheKey, dto, ShellCacheTtl);
        return dto;
    }

    public async Task<DashboardAdminDetalleDto> ObtenerDetalleAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureOficina(oficinaId);
        EnsureConnection();

        var cacheKey = $"dashboard:admin:detalle:{oficinaId}";
        if (cache.TryGetValue(cacheKey, out DashboardAdminDetalleDto? cached) && cached is not null)
        {
            return cached;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                SqlDetalle,
                new { OficinaId = oficinaId },
                commandTimeout: DetalleCommandTimeoutSeconds,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var cartera = await multi.ReadSingleAsync<DashboardAdminCarteraDto>().ConfigureAwait(false);
        var flujo = (await multi.ReadAsync<DashboardAdminFlujoRowDto>().ConfigureAwait(false)).AsList();
        var historico = (await multi.ReadAsync<DashboardAdminHistoricoPuntoDto>().ConfigureAwait(false)).AsList();
        var mensual = (await multi.ReadAsync<DashboardAdminHistoricoMensualDto>().ConfigureAwait(false)).AsList();
        var analistasRaw = (await multi.ReadAsync<AnalistaRow>().ConfigureAwait(false)).AsList();
        var analistas = analistasRaw.Select(MapAnalista).ToList();

        var dto = new DashboardAdminDetalleDto(flujo, historico, mensual, analistas, cartera);
        cache.Set(cacheKey, dto, DetalleCacheTtl);
        return dto;
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private static void EnsureOficina(int oficinaId)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }
    }

    private static DashboardAdminResumenDto MergeCartera(
        DashboardAdminResumenDto resumen,
        DashboardAdminCarteraDto cartera) =>
        resumen with
        {
            TotalClientes = cartera.TotalClientes,
            SaldoCartera = cartera.SaldoCartera,
            SaldoCreditos = cartera.SaldoCreditos,
            SaldoMoraCartera = cartera.SaldoMoraCartera,
            SaldoVencido = cartera.SaldoVencido,
            SaldoMorosidad = cartera.SaldoMorosidad,
            ClientesMora = cartera.ClientesMora
        };

    private static DashboardAdminResumenDto MapResumen(ResumenRow r)
    {
        var flujoHoy = r.EntradasHoy - r.SalidasHoy;
        var flujoAyer = r.EntradasAyer - r.SalidasAyer;
        var flujoAnteayer = r.EntradasAnteayer - r.SalidasAnteayer;
        var flujoMes = r.EntradasMesActual - r.SalidasMesActual;
        var flujoMesAnt = r.EntradasMesAnteriorComparable - r.SalidasMesAnteriorComparable;

        return new DashboardAdminResumenDto(
            r.TotalAnalistas,
            r.TotalClientes,
            r.CreditosHoy,
            r.CreditosAyer,
            r.CreditosAnteayer,
            r.CreditosMesActual,
            r.CreditosMesAnteriorComparable,
            DashboardAnalistaInsights.VariacionPorcentaje(r.CreditosHoy, r.CreditosAyer),
            DashboardAnalistaInsights.VariacionPorcentaje(r.CreditosMesActual, r.CreditosMesAnteriorComparable),
            r.DesembolsoHoy,
            r.DesembolsoAyer,
            r.DesembolsoAnteayer,
            r.DesembolsoMesActual,
            r.DesembolsoMesAnteriorComparable,
            DashboardAnalistaInsights.VariacionPorcentaje(r.DesembolsoHoy, r.DesembolsoAyer),
            DashboardAnalistaInsights.VariacionPorcentaje(r.DesembolsoMesActual, r.DesembolsoMesAnteriorComparable),
            r.CobradoHoy,
            r.CobradoAyer,
            r.CobradoAnteayer,
            r.CobradoMesActual,
            r.CobradoMesAnteriorComparable,
            DashboardAnalistaInsights.VariacionPorcentaje(r.CobradoHoy, r.CobradoAyer),
            DashboardAnalistaInsights.VariacionPorcentaje(r.CobradoMesActual, r.CobradoMesAnteriorComparable),
            r.EntradasHoy,
            r.SalidasHoy,
            flujoHoy,
            r.EntradasAyer,
            r.SalidasAyer,
            flujoAyer,
            r.EntradasAnteayer,
            r.SalidasAnteayer,
            flujoAnteayer,
            r.EntradasMesActual,
            r.SalidasMesActual,
            flujoMes,
            r.EntradasMesAnteriorComparable,
            r.SalidasMesAnteriorComparable,
            flujoMesAnt,
            DashboardAnalistaInsights.VariacionPorcentaje(flujoHoy, flujoAyer),
            DashboardAnalistaInsights.VariacionPorcentaje(flujoMes, flujoMesAnt),
            r.SaldoCartera,
            r.SaldoCreditos,
            r.SaldoMoraCartera,
            r.SaldoVencido,
            r.SaldoMorosidad,
            r.ClientesMora,
            r.CreditosPorVencerSemana);
    }

    private static DashboardAdminAnalistaRowDto MapAnalista(AnalistaRow a)
    {
        var pctMora = a.TotalClientes > 0
            ? Math.Round((a.ClientesMora / (decimal)a.TotalClientes) * 100m, 1, MidpointRounding.AwayFromZero)
            : 0m;
        return new DashboardAdminAnalistaRowDto(
            a.UsuarioId,
            string.IsNullOrWhiteSpace(a.NombreCompleto) ? "Analista" : a.NombreCompleto.Trim(),
            a.TotalClientes,
            a.ClientesNuevosMes,
            a.ColocacionesHoy,
            a.ColocacionesMes,
            a.DesembolsoHoy,
            a.DesembolsoMes,
            a.CobradoHoy,
            a.CobradoMes,
            a.CobradoMesAnteriorComparable,
            DashboardAnalistaInsights.VariacionPorcentaje(a.CobradoMes, a.CobradoMesAnteriorComparable),
            a.ClientesMora,
            a.MontoMora,
            pctMora);
    }

    private sealed class ResumenRow
    {
        public string NombreOficina { get; init; } = "Oficina";
        public DateTime FechaConsulta { get; init; }
        public int TotalAnalistas { get; init; }
        public int TotalClientes { get; init; }
        public int CreditosHoy { get; init; }
        public int CreditosAyer { get; init; }
        public int CreditosAnteayer { get; init; }
        public int CreditosMesActual { get; init; }
        public int CreditosMesAnteriorComparable { get; init; }
        public decimal DesembolsoHoy { get; init; }
        public decimal DesembolsoAyer { get; init; }
        public decimal DesembolsoAnteayer { get; init; }
        public decimal DesembolsoMesActual { get; init; }
        public decimal DesembolsoMesAnteriorComparable { get; init; }
        public decimal CobradoHoy { get; init; }
        public decimal CobradoAyer { get; init; }
        public decimal CobradoAnteayer { get; init; }
        public decimal CobradoMesActual { get; init; }
        public decimal CobradoMesAnteriorComparable { get; init; }
        public decimal EntradasHoy { get; init; }
        public decimal SalidasHoy { get; init; }
        public decimal EntradasAyer { get; init; }
        public decimal SalidasAyer { get; init; }
        public decimal EntradasAnteayer { get; init; }
        public decimal SalidasAnteayer { get; init; }
        public decimal EntradasMesActual { get; init; }
        public decimal SalidasMesActual { get; init; }
        public decimal EntradasMesAnteriorComparable { get; init; }
        public decimal SalidasMesAnteriorComparable { get; init; }
        public decimal SaldoCartera { get; init; }
        public decimal SaldoCreditos { get; init; }
        public decimal SaldoMoraCartera { get; init; }
        public decimal SaldoVencido { get; init; }
        public decimal SaldoMorosidad { get; init; }
        public int ClientesMora { get; init; }
        public int CreditosPorVencerSemana { get; init; }
    }

    private sealed class AnalistaRow
    {
        public int UsuarioId { get; init; }
        public string NombreCompleto { get; init; } = "";
        public int TotalClientes { get; init; }
        public int ClientesNuevosMes { get; init; }
        public int ColocacionesHoy { get; init; }
        public int ColocacionesMes { get; init; }
        public decimal DesembolsoHoy { get; init; }
        public decimal DesembolsoMes { get; init; }
        public decimal CobradoHoy { get; init; }
        public decimal CobradoMes { get; init; }
        public decimal CobradoMesAnteriorComparable { get; init; }
        public int ClientesMora { get; init; }
        public decimal MontoMora { get; init; }
    }
}
