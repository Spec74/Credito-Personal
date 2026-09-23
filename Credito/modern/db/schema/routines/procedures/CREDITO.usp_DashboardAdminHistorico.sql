
CREATE   PROCEDURE [CREDITO].[usp_DashboardAdminHistorico]
(
    @FechaCorte DATE = NULL,
    @Dias INT = 30
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- PARÁMETROS
    ------------------------------------------------------------
    DECLARE @Hoy DATE =
        ISNULL(@FechaCorte, CAST(GETDATE() AS DATE));

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;

    IF @Dias > 365
        SET @Dias = 365;

    DECLARE @Manana DATE = DATEADD(DAY, 1, @Hoy);
    DECLARE @FechaInicio DATE = DATEADD(DAY, -(@Dias - 1), @Hoy);

    ------------------------------------------------------------
    -- MATERIALIZAR CALENDARIO
    ------------------------------------------------------------
    CREATE TABLE #Fechas
    (
        Fecha DATE NOT NULL PRIMARY KEY
    );

    INSERT INTO #Fechas (Fecha)
    SELECT
        DATEADD(DAY, N.N, @FechaInicio)
    FROM
    (
        SELECT TOP (@Dias)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS N
        FROM sys.all_objects
    ) N;

    ------------------------------------------------------------
    -- MATERIALIZAR COLOCACIONES / DESEMBOLSOS
    -- Una sola lectura de CREDITO.Credito
    ------------------------------------------------------------
    CREATE TABLE #Colocaciones
    (
        Fecha DATE NOT NULL PRIMARY KEY,
        Colocaciones INT NOT NULL,
        Desembolsado DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #Colocaciones
    (
        Fecha,
        Colocaciones,
        Desembolsado
    )
    SELECT
        CAST(C.FechaDesembolso AS DATE),
        COUNT(*),
        CAST(SUM(ISNULL(C.MontoDesembolso, 0)) AS DECIMAL(18,2))
    FROM CREDITO.Credito C
    WHERE
        C.FechaDesembolso >= @FechaInicio
        AND C.FechaDesembolso < @Manana
        AND C.Estado IN ('DES', 'PAG', 'REP')
    GROUP BY
        CAST(C.FechaDesembolso AS DATE);

    ------------------------------------------------------------
    -- MATERIALIZAR MOVIMIENTOS
    -- IMPORTANTE:
    -- El CTE anterior podía ser re-evaluado una vez por cada fecha
    -- del calendario. Esta tabla temporal obliga a agregar
    -- MovimientoCaja una sola vez para todo el rango solicitado.
    ------------------------------------------------------------
    CREATE TABLE #Movimientos
    (
        Fecha DATE NOT NULL PRIMARY KEY,
        Cobrado DECIMAL(18,2) NOT NULL,
        Entradas DECIMAL(18,2) NOT NULL,
        Salidas DECIMAL(18,2) NOT NULL,
        TransferenciaEntrada DECIMAL(18,2) NOT NULL,
        TransferenciaSalida DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #Movimientos
    (
        Fecha,
        Cobrado,
        Entradas,
        Salidas,
        TransferenciaEntrada,
        TransferenciaSalida
    )
    SELECT
        CAST(M.FechaReg AS DATE),

        CAST(SUM(
            CASE
                WHEN M.Operacion = 'CUO'
                THEN M.ImportePago
                ELSE 0
            END
        ) AS DECIMAL(18,2)),

        CAST(SUM(
            CASE
                WHEN M.IndEntrada = 1
                THEN M.ImportePago
                ELSE 0
            END
        ) AS DECIMAL(18,2)),

        CAST(SUM(
            CASE
                WHEN M.IndEntrada = 0
                THEN M.ImportePago
                ELSE 0
            END
        ) AS DECIMAL(18,2)),

        CAST(SUM(
            CASE
                WHEN M.Operacion = 'TRE'
                THEN M.ImportePago
                ELSE 0
            END
        ) AS DECIMAL(18,2)),

        CAST(SUM(
            CASE
                WHEN M.Operacion = 'TRS'
                THEN M.ImportePago
                ELSE 0
            END
        ) AS DECIMAL(18,2))

    FROM CREDITO.MovimientoCaja M
    WHERE
        M.Estado = 1
        AND M.FechaReg >= @FechaInicio
        AND M.FechaReg < @Manana
    GROUP BY
        CAST(M.FechaReg AS DATE);

    ------------------------------------------------------------
    -- RESULTADO
    -- Mismo contrato que la versión anterior
    ------------------------------------------------------------
    SELECT
        F.Fecha,

        ISNULL(C.Colocaciones, 0) AS Colocaciones,

        CAST(
            ISNULL(C.Desembolsado, 0)
            AS DECIMAL(18,2)
        ) AS Desembolsado,

        CAST(
            ISNULL(M.Cobrado, 0)
            AS DECIMAL(18,2)
        ) AS Cobrado,

        CAST(
            ISNULL(M.Entradas, 0)
            AS DECIMAL(18,2)
        ) AS Entradas,

        CAST(
            ISNULL(M.Salidas, 0)
            AS DECIMAL(18,2)
        ) AS Salidas,

        CAST(
            ISNULL(M.Entradas, 0)
            -
            ISNULL(M.Salidas, 0)
            AS DECIMAL(18,2)
        ) AS FlujoNeto,

        CAST(
            (
                ISNULL(M.Entradas, 0)
                -
                ISNULL(M.TransferenciaEntrada, 0)
            )
            -
            (
                ISNULL(M.Salidas, 0)
                -
                ISNULL(M.TransferenciaSalida, 0)
            )
            AS DECIMAL(18,2)
        ) AS FlujoOperativo

    FROM #Fechas F

    LEFT JOIN #Colocaciones C
        ON C.Fecha = F.Fecha

    LEFT JOIN #Movimientos M
        ON M.Fecha = F.Fecha

    ORDER BY
        F.Fecha;
END;

