

CREATE PROCEDURE [CREDITO].[usp_DashboardAdminAnalistas]
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

    DECLARE @InicioMesActual DATE =
        DATEFROMPARTS(
            YEAR(@Hoy),
            MONTH(@Hoy),
            1
        );

    DECLARE @InicioMesAnterior DATE =
        DATEADD(
            MONTH,
            -1,
            @InicioMesActual
        );

    DECLARE @DiasComparables INT =
        CASE
            WHEN DAY(@Hoy) >
                 DAY(EOMONTH(@InicioMesAnterior))
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
    -- ANALISTAS ACTIVOS
    ------------------------------------------------------------

    ;WITH Analistas AS
    (
        SELECT DISTINCT
            U.UsuarioId,
            U.NombreUsuario,
            P.NombreCompleto

        FROM MAESTRO.Usuario U

        INNER JOIN MAESTRO.UsuarioRol UR
            ON UR.UsuarioId = U.UsuarioId

        LEFT JOIN MAESTRO.Persona P
            ON P.PersonaId = U.PersonaId

        WHERE
            U.Estado = 1
            AND UR.RolId = 6
            AND U.NombreUsuario <> 'IRRECUPERABLE'
    ),


    ------------------------------------------------------------
    -- PRIMERA COLOCACIÓN DE CADA CLIENTE CON CADA ANALISTA
    ------------------------------------------------------------

    PrimeraColocacionCliente AS
    (
        SELECT
            C.UsuarioRegId,
            C.PersonaId,
            MIN(C.FechaDesembolso) AS PrimeraFechaDesembolso

        FROM CREDITO.Credito C

        WHERE
            C.UsuarioRegId IS NOT NULL
            AND C.PersonaId IS NOT NULL
            AND C.FechaDesembolso IS NOT NULL
            AND C.Estado IN ('DES', 'PAG', 'REP')

        GROUP BY
            C.UsuarioRegId,
            C.PersonaId
    ),


    ------------------------------------------------------------
    -- CLIENTES ACTIVOS POR ANALISTA
    ------------------------------------------------------------

    Clientes AS
    (
        SELECT
            C.UsuarioRegId,

            COUNT(
                DISTINCT
                CASE
                    WHEN C.Estado = 'DES'
                     AND ISNULL(C.IndIrrecuperable, 0) = 0
                    THEN C.PersonaId
                END
            ) AS TotalClientes

        FROM CREDITO.Credito C

        GROUP BY
            C.UsuarioRegId
    ),


    ------------------------------------------------------------
    -- CLIENTES NUEVOS DEL MES
    ------------------------------------------------------------

    ClientesNuevos AS
    (
        SELECT
            PC.UsuarioRegId,
            COUNT(*) AS ClientesNuevosMes

        FROM PrimeraColocacionCliente PC

        WHERE
            PC.PrimeraFechaDesembolso >= @InicioMesActual
            AND PC.PrimeraFechaDesembolso < @Manana

        GROUP BY
            PC.UsuarioRegId
    ),


    ------------------------------------------------------------
    -- COLOCACIONES Y DESEMBOLSOS
    ------------------------------------------------------------

    Colocaciones AS
    (
        SELECT
            C.UsuarioRegId,

            SUM(
                CASE
                    WHEN C.FechaDesembolso >= @Hoy
                     AND C.FechaDesembolso < @Manana
                    THEN 1
                    ELSE 0
                END
            ) AS ColocacionesHoy,

            SUM(
                CASE
                    WHEN C.FechaDesembolso >= @InicioMesActual
                     AND C.FechaDesembolso < @Manana
                    THEN 1
                    ELSE 0
                END
            ) AS ColocacionesMes,

            SUM(
                CASE
                    WHEN C.FechaDesembolso >= @Hoy
                     AND C.FechaDesembolso < @Manana
                    THEN ISNULL(C.MontoDesembolso, 0)
                    ELSE 0
                END
            ) AS DesembolsoHoy,

            SUM(
                CASE
                    WHEN C.FechaDesembolso >= @InicioMesActual
                     AND C.FechaDesembolso < @Manana
                    THEN ISNULL(C.MontoDesembolso, 0)
                    ELSE 0
                END
            ) AS DesembolsoMes

        FROM CREDITO.Credito C

        WHERE
            C.FechaDesembolso IS NOT NULL
            AND C.Estado IN ('DES', 'PAG', 'REP')

        GROUP BY
            C.UsuarioRegId
    ),


    ------------------------------------------------------------
    -- COBRANZA
    --
    -- Se atribuye al analista propietario del crédito.
    ------------------------------------------------------------

    Cobranza AS
    (
        SELECT
            C.UsuarioRegId,

            SUM(
                CASE
                    WHEN M.FechaReg >= @Hoy
                     AND M.FechaReg < @Manana
                    THEN M.ImportePago
                    ELSE 0
                END
            ) AS CobradoHoy,

            SUM(
                CASE
                    WHEN M.FechaReg >= @InicioMesActual
                     AND M.FechaReg < @Manana
                    THEN M.ImportePago
                    ELSE 0
                END
            ) AS CobradoMes,

            SUM(
                CASE
                    WHEN M.FechaReg >= @InicioMesAnterior
                     AND M.FechaReg < @FinMesAnteriorComparable
                    THEN M.ImportePago
                    ELSE 0
                END
            ) AS CobradoMesAnteriorComparable

        FROM CREDITO.MovimientoCaja M

        INNER JOIN CREDITO.Credito C
            ON C.CreditoId = M.CreditoId

        WHERE
            M.Operacion = 'CUO'
            AND M.Estado = 1
            AND M.ImportePago > 0

        GROUP BY
            C.UsuarioRegId
    ),


    ------------------------------------------------------------
    -- CLIENTES EN MORA
    --
    -- IMPORTANTE:
    -- Conservamos la definición ORIGINAL del Dashboard.
    --
    -- Un cliente entra en este indicador cuando posee al menos
    -- una cuota PEN cuya fecha de vencimiento ya pasó.
    --
    -- Esta métrica es la que alimenta PorcentajeMora.
    ------------------------------------------------------------

    ClientesMora AS
    (
        SELECT
            C.UsuarioRegId,

            COUNT(
                DISTINCT C.PersonaId
            ) AS ClientesMora

        FROM CREDITO.Credito C

        WHERE
            C.Estado = 'DES'

            AND ISNULL(
                C.IndIrrecuperable,
                0
            ) = 0

            AND EXISTS
            (
                SELECT 1

                FROM CREDITO.PlanPago PP

                WHERE
                    PP.CreditoId = C.CreditoId
                    AND PP.Estado = 'PEN'
                    AND PP.FechaVencimiento < @Hoy
            )

        GROUP BY
            C.UsuarioRegId
    ),


    ------------------------------------------------------------
    -- CRÉDITOS PARA SALDO DE MORA
    --
    -- Esta segunda definición NO reemplaza ClientesMora.
    --
    -- Su único objetivo es obtener el MONTO equivalente a
    -- SaldoMoraCartera utilizando la lógica de Bóveda /
    -- usp_ListarSaldoCartera.
    ------------------------------------------------------------

    CreditosCarteraMora AS
    (
        SELECT
            C.CreditoId,
            C.UsuarioRegId,
            C.PersonaId,
            C.FechaVencimiento

        FROM CREDITO.Credito C

        WHERE
            C.Estado = 'DES'

            /*
                No filtramos IndIrrecuperable aquí porque
                usp_ListarSaldoCartera tampoco lo hace.

                Así MontoMora mantiene la misma semántica
                financiera de la cartilla de cartera.
            */

            AND C.FechaDesembolso < @Manana
    ),


    ------------------------------------------------------------
    -- TOTAL DEL PLAN DE PAGO
    ------------------------------------------------------------

    PlanPagoCartera AS
    (
        SELECT
            PP.CreditoId,

            SUM(
                ISNULL(PP.Cuota, 0)
                +
                ISNULL(PP.Cargo, 0)
            ) AS MontoTotal

        FROM CREDITO.PlanPago PP

        INNER JOIN CreditosCarteraMora C
            ON C.CreditoId = PP.CreditoId

        GROUP BY
            PP.CreditoId
    ),


    ------------------------------------------------------------
    -- PAGOS REALIZADOS
    ------------------------------------------------------------

    PagosCartera AS
    (
        SELECT
            M.CreditoId,

            SUM(
                M.ImportePago
            ) AS MovCaja

        FROM CREDITO.MovimientoCaja M

        INNER JOIN CreditosCarteraMora C
            ON C.CreditoId = M.CreditoId

        WHERE
            M.ImportePago > 0
            AND M.Operacion = 'CUO'
            AND M.Estado = 1

            /*
                Mantiene reproducibilidad de @FechaCorte.
            */
            AND M.FechaReg < @Manana

        GROUP BY
            M.CreditoId
    ),


    ------------------------------------------------------------
    -- SALDO DE CADA CRÉDITO
    ------------------------------------------------------------

    CarteraMoraBase AS
    (
        SELECT
            C.CreditoId,
            C.UsuarioRegId,
            C.PersonaId,

            CAST(
                ISNULL(PP.MontoTotal, 0)
                -
                ISNULL(PG.MovCaja, 0)

                AS DECIMAL(18,2)
            ) AS Saldo,

            /*
                Versión set-based de la lógica de días de atraso.
                Evita ejecutar el WHILE de dbo.ufnCalcularDiasAtrazo()
                para cada crédito activo.
            */
            CASE
                WHEN C.FechaVencimiento IS NULL
                    THEN NULL

                WHEN C.FechaVencimiento >= @Hoy
                    THEN 0

                ELSE
                    DATEDIFF(
                        DAY,
                        C.FechaVencimiento,
                        @Hoy
                    )
                    -
                    DATEDIFF(
                        WEEK,
                        DATEADD(
                            DAY,
                            -1,
                            C.FechaVencimiento
                        ),
                        @Hoy
                    )
            END AS DiasAtrazoMora

        FROM CreditosCarteraMora C

        LEFT JOIN PlanPagoCartera PP
            ON PP.CreditoId = C.CreditoId

        LEFT JOIN PagosCartera PG
            ON PG.CreditoId = C.CreditoId
    ),


    ------------------------------------------------------------
    -- MONTO DE MORA POR ANALISTA
    --
    -- Con @DiasGracia = 0:
    -- ufnCalcularMora(Saldo, DiasAtrazo, 0) > 0
    -- equivale a Saldo > 0 AND DiasAtrazo > 0.
    ------------------------------------------------------------

    MontoMoraAnalista AS
    (
        SELECT
            CMB.UsuarioRegId,

            CAST(
                ISNULL(
                    SUM(
                        CASE
                            WHEN CMB.Saldo > 0
                             AND CMB.DiasAtrazoMora > 0
                            THEN CMB.Saldo
                            ELSE 0
                        END
                    ),
                    0
                )

                AS DECIMAL(18,2)
            ) AS MontoMora

        FROM CarteraMoraBase CMB

        GROUP BY
            CMB.UsuarioRegId
    )


    ------------------------------------------------------------
    -- RESULTADO
    ------------------------------------------------------------

    SELECT
        A.UsuarioId,
        A.NombreUsuario,
        A.NombreCompleto,


        --------------------------------------------------------
        -- CLIENTES
        --------------------------------------------------------

        ISNULL(CL.TotalClientes, 0)
            AS TotalClientes,

        ISNULL(CN.ClientesNuevosMes, 0)
            AS ClientesNuevosMes,


        --------------------------------------------------------
        -- COLOCACIONES
        --------------------------------------------------------

        ISNULL(CO.ColocacionesHoy, 0)
            AS ColocacionesHoy,

        ISNULL(CO.ColocacionesMes, 0)
            AS ColocacionesMes,


        --------------------------------------------------------
        -- DESEMBOLSOS
        --------------------------------------------------------

        CAST(
            ISNULL(CO.DesembolsoHoy, 0)
            AS DECIMAL(18,2)
        )
            AS DesembolsoHoy,

        CAST(
            ISNULL(CO.DesembolsoMes, 0)
            AS DECIMAL(18,2)
        )
            AS DesembolsoMes,


        --------------------------------------------------------
        -- COBRANZA
        --------------------------------------------------------

        CAST(
            ISNULL(CB.CobradoHoy, 0)
            AS DECIMAL(18,2)
        )
            AS CobradoHoy,

        CAST(
            ISNULL(CB.CobradoMes, 0)
            AS DECIMAL(18,2)
        )
            AS CobradoMes,

        CAST(
            ISNULL(
                CB.CobradoMesAnteriorComparable,
                0
            )
            AS DECIMAL(18,2)
        )
            AS CobradoMesAnteriorComparable,


        --------------------------------------------------------
        -- VARIACIÓN DE COBRANZA
        --------------------------------------------------------

        CAST(
            CASE
                WHEN ISNULL(
                    CB.CobradoMesAnteriorComparable,
                    0
                ) = 0
                THEN NULL

                ELSE
                    (
                        (
                            ISNULL(CB.CobradoMes, 0)
                            -
                            CB.CobradoMesAnteriorComparable
                        )
                        /
                        ABS(
                            CB.CobradoMesAnteriorComparable
                        )
                    ) * 100
            END

            AS DECIMAL(18,2)
        )
            AS VariacionCobranza,


        --------------------------------------------------------
        -- MORA: CANTIDAD DE CLIENTES
        --------------------------------------------------------

        ISNULL(
            CM.ClientesMora,
            0
        )
            AS ClientesMora,


        --------------------------------------------------------
        -- MORA: SALDO MONETARIO
        --------------------------------------------------------

        CAST(
            ISNULL(
                MMA.MontoMora,
                0
            )
            AS DECIMAL(18,2)
        )
            AS MontoMora,


        --------------------------------------------------------
        -- PORCENTAJE DE CLIENTES CON ATRASO
        --------------------------------------------------------

        CAST(
            CASE
                WHEN ISNULL(
                    CL.TotalClientes,
                    0
                ) = 0
                THEN 0

                ELSE
                    (
                        CAST(
                            ISNULL(
                                CM.ClientesMora,
                                0
                            )
                            AS DECIMAL(18,4)
                        )
                        /
                        CL.TotalClientes
                    ) * 100
            END

            AS DECIMAL(18,2)
        )
            AS PorcentajeMora


    FROM Analistas A

    LEFT JOIN Clientes CL
        ON CL.UsuarioRegId = A.UsuarioId

    LEFT JOIN ClientesNuevos CN
        ON CN.UsuarioRegId = A.UsuarioId

    LEFT JOIN Colocaciones CO
        ON CO.UsuarioRegId = A.UsuarioId

    LEFT JOIN Cobranza CB
        ON CB.UsuarioRegId = A.UsuarioId

    LEFT JOIN ClientesMora CM
        ON CM.UsuarioRegId = A.UsuarioId

    LEFT JOIN MontoMoraAnalista MMA
        ON MMA.UsuarioRegId = A.UsuarioId


    ------------------------------------------------------------
    -- RANKING
    ------------------------------------------------------------

    ORDER BY
        ISNULL(
            CB.CobradoMes,
            0
        ) DESC,

        A.NombreCompleto ASC;

END;

