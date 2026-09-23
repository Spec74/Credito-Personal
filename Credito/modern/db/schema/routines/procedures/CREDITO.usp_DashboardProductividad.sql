
CREATE PROCEDURE [CREDITO].[usp_DashboardProductividad]
(
    @UsuarioId INT,
    @OficinaId INT
)
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Hoy DATE = dbo.ufnFecha();
    DECLARE @InicioPeriodo DATE = DATEADD(DAY, -29, @Hoy);
    DECLARE @Manana DATE = DATEADD(DAY, 1, @Hoy);

    ;WITH Numeros AS
    (
        SELECT 0 AS N
        UNION ALL
        SELECT N + 1
        FROM Numeros
        WHERE N < 29
    ),
    Dias AS
    (
        SELECT DATEADD(DAY, N, @InicioPeriodo) AS Fecha
        FROM Numeros
    ),
    Cobranza AS
    (
        SELECT
            CAST(M.FechaReg AS DATE) AS Fecha,
            SUM(M.ImportePago) AS MontoCobrado
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CREDITO.Credito C
            ON C.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO'
          AND M.Estado = 1
          AND M.ImportePago > 0
          AND C.UsuarioRegId = @UsuarioId
          AND C.OficinaId = @OficinaId
          AND M.FechaReg >= @InicioPeriodo
          AND M.FechaReg < @Manana
        GROUP BY CAST(M.FechaReg AS DATE)
    )
    SELECT
        YEAR(D.Fecha) AS Anio,
        MONTH(D.Fecha) AS Mes,
        CONVERT(CHAR(5), D.Fecha, 103) AS NombreMes,
        CAST(ISNULL(C.MontoCobrado, 0) AS DECIMAL(18,2)) AS MontoCobrado
    FROM Dias D
    LEFT JOIN Cobranza C ON C.Fecha = D.Fecha
    ORDER BY D.Fecha
    OPTION (MAXRECURSION 30);
END

