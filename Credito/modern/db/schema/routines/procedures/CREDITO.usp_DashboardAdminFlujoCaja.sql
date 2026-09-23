

CREATE   PROCEDURE [CREDITO].[usp_DashboardAdminFlujoCaja]
(
    @FechaCorte DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- FECHAS
    ------------------------------------------------------------

    DECLARE @Hoy DATE =
        ISNULL(@FechaCorte, CAST(GETDATE() AS DATE));

    DECLARE @Manana DATE =
        DATEADD(DAY, 1, @Hoy);

    DECLARE @Ayer DATE =
        DATEADD(DAY, -1, @Hoy);

    DECLARE @InicioMesActual DATE =
        DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);

    DECLARE @InicioMesAnterior DATE =
        DATEADD(MONTH, -1, @InicioMesActual);

    DECLARE @DiasComparables INT =
        CASE
            WHEN DAY(@Hoy) > DAY(EOMONTH(@InicioMesAnterior))
                THEN DAY(EOMONTH(@InicioMesAnterior))
            ELSE DAY(@Hoy)
        END;

    DECLARE @FinMesAnteriorComparable DATE =
        DATEADD(
            DAY,
            @DiasComparables,
            @InicioMesAnterior
        );


    ------------------------------------------------------------
    -- AGRUPAR MOVIMIENTOS POR OPERACIÓN Y DIRECCIÓN
    ------------------------------------------------------------

    SELECT
        M.Operacion,

        CAST(M.IndEntrada AS BIT)
            AS IndEntrada,

        CASE
            WHEN M.Operacion = 'CUO' THEN 'Cobranza'
            WHEN M.Operacion = 'DES' THEN 'Desembolso'
            WHEN M.Operacion = 'TRE' THEN 'Transferencia entrada'
            WHEN M.Operacion = 'TRS' THEN 'Transferencia salida'
            ELSE M.Operacion
        END AS Concepto,

        CASE
            WHEN M.Operacion IN ('TRE', 'TRS')
                THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS EsTransferencia,


        --------------------------------------------------------
        -- HOY
        --------------------------------------------------------

        COUNT(
            CASE
                WHEN M.FechaReg >= @Hoy
                 AND M.FechaReg < @Manana
                THEN 1
            END
        ) AS CantidadHoy,

        CAST(
            ISNULL(
                SUM(
                    CASE
                        WHEN M.FechaReg >= @Hoy
                         AND M.FechaReg < @Manana
                        THEN M.ImportePago
                        ELSE 0
                    END
                ),
                0
            )
            AS DECIMAL(18,2)
        ) AS ImporteHoy,


        --------------------------------------------------------
        -- AYER
        --------------------------------------------------------

        COUNT(
            CASE
                WHEN M.FechaReg >= @Ayer
                 AND M.FechaReg < @Hoy
                THEN 1
            END
        ) AS CantidadAyer,

        CAST(
            ISNULL(
                SUM(
                    CASE
                        WHEN M.FechaReg >= @Ayer
                         AND M.FechaReg < @Hoy
                        THEN M.ImportePago
                        ELSE 0
                    END
                ),
                0
            )
            AS DECIMAL(18,2)
        ) AS ImporteAyer,


        --------------------------------------------------------
        -- MES ACTUAL HASTA FECHA DE CORTE
        --------------------------------------------------------

        COUNT(
            CASE
                WHEN M.FechaReg >= @InicioMesActual
                 AND M.FechaReg < @Manana
                THEN 1
            END
        ) AS CantidadMesActual,

        CAST(
            ISNULL(
                SUM(
                    CASE
                        WHEN M.FechaReg >= @InicioMesActual
                         AND M.FechaReg < @Manana
                        THEN M.ImportePago
                        ELSE 0
                    END
                ),
                0
            )
            AS DECIMAL(18,2)
        ) AS ImporteMesActual,


        --------------------------------------------------------
        -- MISMO PERÍODO DEL MES ANTERIOR
        --------------------------------------------------------

        COUNT(
            CASE
                WHEN M.FechaReg >= @InicioMesAnterior
                 AND M.FechaReg < @FinMesAnteriorComparable
                THEN 1
            END
        ) AS CantidadMesAnteriorComparable,

        CAST(
            ISNULL(
                SUM(
                    CASE
                        WHEN M.FechaReg >= @InicioMesAnterior
                         AND M.FechaReg < @FinMesAnteriorComparable
                        THEN M.ImportePago
                        ELSE 0
                    END
                ),
                0
            )
            AS DECIMAL(18,2)
        ) AS ImporteMesAnteriorComparable

    FROM CREDITO.MovimientoCaja M

    WHERE
        M.Estado = 1

        /*
            Evitamos recorrer innecesariamente todo el histórico.
            Solo necesitamos ayer, mes actual y mes anterior.
        */
        AND M.FechaReg >=
            CASE
                WHEN @Ayer < @InicioMesAnterior
                    THEN @Ayer
                ELSE @InicioMesAnterior
            END

        AND M.FechaReg < @Manana

    GROUP BY
        M.Operacion,
        M.IndEntrada

    ORDER BY
        M.IndEntrada DESC,
        ImporteMesActual DESC,
        M.Operacion ASC;

END;

