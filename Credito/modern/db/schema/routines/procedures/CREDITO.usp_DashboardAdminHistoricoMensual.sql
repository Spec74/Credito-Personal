
CREATE   PROCEDURE [CREDITO].[usp_DashboardAdminHistoricoMensual]
(
    @FechaCorte DATE = NULL,
    @Meses INT = 12
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- PARÁMETROS
    ------------------------------------------------------------
    DECLARE @Hoy DATE =
        ISNULL(@FechaCorte, CAST(GETDATE() AS DATE));

    IF @Meses IS NULL OR @Meses < 1
        SET @Meses = 12;

    IF @Meses > 36
        SET @Meses = 36;

    DECLARE @Manana DATE =
        DATEADD(DAY, 1, @Hoy);

    DECLARE @InicioMesActual DATE =
        DATEFROMPARTS(
            YEAR(@Hoy),
            MONTH(@Hoy),
            1
        );

    DECLARE @InicioHistorico DATE =
        DATEADD(
            MONTH,
            -(@Meses - 1),
            @InicioMesActual
        );

    ------------------------------------------------------------
    -- MATERIALIZAR CALENDARIO DE MESES
    ------------------------------------------------------------
    CREATE TABLE #Meses
    (
        FechaMes DATE NOT NULL PRIMARY KEY
    );

    INSERT INTO #Meses (FechaMes)
    SELECT
        DATEADD(MONTH, N.N, @InicioHistorico)
    FROM
    (
        SELECT TOP (@Meses)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS N
        FROM sys.all_objects
    ) N;

    ------------------------------------------------------------
    -- MATERIALIZAR COLOCACIONES / DESEMBOLSOS
    -- Una sola lectura de CREDITO.Credito
    ------------------------------------------------------------
    CREATE TABLE #Colocaciones
    (
        FechaMes DATE NOT NULL PRIMARY KEY,
        Colocaciones INT NOT NULL,
        Desembolsado DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #Colocaciones
    (
        FechaMes,
        Colocaciones,
        Desembolsado
    )
    SELECT
        DATEFROMPARTS(
            YEAR(C.FechaDesembolso),
            MONTH(C.FechaDesembolso),
            1
        ),
        COUNT(*),
        CAST(SUM(ISNULL(C.MontoDesembolso, 0)) AS DECIMAL(18,2))
    FROM CREDITO.Credito C
    WHERE
        C.FechaDesembolso >= @InicioHistorico
        AND C.FechaDesembolso < @Manana
        AND C.Estado IN ('DES', 'PAG', 'REP')
    GROUP BY
        DATEFROMPARTS(
            YEAR(C.FechaDesembolso),
            MONTH(C.FechaDesembolso),
            1
        );

    ------------------------------------------------------------
    -- MATERIALIZAR MOVIMIENTOS
    -- Evita que el agregado mensual sea re-evaluado por cada
    -- fila del calendario de meses.
    ------------------------------------------------------------
    CREATE TABLE #Movimientos
    (
        FechaMes DATE NOT NULL PRIMARY KEY,
        Cobrado DECIMAL(18,2) NOT NULL,
        Entradas DECIMAL(18,2) NOT NULL,
        Salidas DECIMAL(18,2) NOT NULL,
        TransferenciasEntrada DECIMAL(18,2) NOT NULL,
        TransferenciasSalida DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #Movimientos
    (
        FechaMes,
        Cobrado,
        Entradas,
        Salidas,
        TransferenciasEntrada,
        TransferenciasSalida
    )
    SELECT
        DATEFROMPARTS(
            YEAR(M.FechaReg),
            MONTH(M.FechaReg),
            1
        ),

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
        AND M.FechaReg >= @InicioHistorico
        AND M.FechaReg < @Manana
    GROUP BY
        DATEFROMPARTS(
            YEAR(M.FechaReg),
            MONTH(M.FechaReg),
            1
        );

    ------------------------------------------------------------
    -- RESULTADO
    -- Mismo contrato que la versión anterior
    ------------------------------------------------------------
    SELECT
        ME.FechaMes,

        CAST(
            CASE
                WHEN ME.FechaMes = @InicioMesActual
                    THEN 1
                ELSE 0
            END
            AS BIT
        ) AS EsMesActual,

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
                ISNULL(M.TransferenciasEntrada, 0)
            )
            -
            (
                ISNULL(M.Salidas, 0)
                -
                ISNULL(M.TransferenciasSalida, 0)
            )
            AS DECIMAL(18,2)
        ) AS FlujoOperativo

    FROM #Meses ME

    LEFT JOIN #Colocaciones C
        ON C.FechaMes = ME.FechaMes

    LEFT JOIN #Movimientos M
        ON M.FechaMes = ME.FechaMes

    ORDER BY
        ME.FechaMes;
END;

