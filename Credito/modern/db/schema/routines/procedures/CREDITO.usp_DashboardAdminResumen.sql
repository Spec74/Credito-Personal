

CREATE   PROCEDURE [CREDITO].[usp_DashboardAdminResumen]
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
        ISNULL(
            @FechaCorte,
            CAST(GETDATE() AS DATE)
        );

    DECLARE @Manana DATE =
        DATEADD(DAY, 1, @Hoy);

    DECLARE @Ayer DATE =
        DATEADD(DAY, -1, @Hoy);

    DECLARE @Anteayer DATE =
        DATEADD(DAY, -2, @Hoy);

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


    /*
        Si hoy es 13/08:
        comparamos 01-13 Ago contra 01-13 Jul.

        Trabajamos siempre con rangos semiabiertos:

        >= FechaInicio
        <  FechaFin
    */

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
    -- VARIABLES GENERALES
    ------------------------------------------------------------

    DECLARE @TotalAnalistas INT = 0;
    DECLARE @TotalClientes INT = 0;


    ------------------------------------------------------------
    -- COLOCACIONES
    ------------------------------------------------------------

    DECLARE @CreditosHoy INT = 0;
    DECLARE @CreditosAyer INT = 0;
    DECLARE @CreditosAnteayer INT = 0;

    DECLARE @CreditosMesActual INT = 0;
    DECLARE @CreditosMesAnteriorComparable INT = 0;


    ------------------------------------------------------------
    -- DESEMBOLSOS
    ------------------------------------------------------------

    DECLARE @DesembolsoHoy DECIMAL(18,2) = 0;
    DECLARE @DesembolsoAyer DECIMAL(18,2) = 0;
    DECLARE @DesembolsoAnteayer DECIMAL(18,2) = 0;

    DECLARE @DesembolsoMesActual DECIMAL(18,2) = 0;
    DECLARE @DesembolsoMesAnteriorComparable DECIMAL(18,2) = 0;


    ------------------------------------------------------------
    -- COBRANZA
    ------------------------------------------------------------

    DECLARE @CobradoHoy DECIMAL(18,2) = 0;
    DECLARE @CobradoAyer DECIMAL(18,2) = 0;
    DECLARE @CobradoAnteayer DECIMAL(18,2) = 0;

    DECLARE @CobradoMesActual DECIMAL(18,2) = 0;
    DECLARE @CobradoMesAnteriorComparable DECIMAL(18,2) = 0;


    ------------------------------------------------------------
    -- FLUJO DE CAJA
    ------------------------------------------------------------

    DECLARE @EntradasHoy DECIMAL(18,2) = 0;
    DECLARE @SalidasHoy DECIMAL(18,2) = 0;

    DECLARE @EntradasAyer DECIMAL(18,2) = 0;
    DECLARE @SalidasAyer DECIMAL(18,2) = 0;

    DECLARE @EntradasAnteayer DECIMAL(18,2) = 0;
    DECLARE @SalidasAnteayer DECIMAL(18,2) = 0;

    DECLARE @EntradasMesActual DECIMAL(18,2) = 0;
    DECLARE @SalidasMesActual DECIMAL(18,2) = 0;

    DECLARE @EntradasMesAnteriorComparable DECIMAL(18,2) = 0;
    DECLARE @SalidasMesAnteriorComparable DECIMAL(18,2) = 0;


    ------------------------------------------------------------
    -- CARTERA
    --
    -- Misma semántica utilizada por:
    -- CREDITO.usp_ListarSaldoCartera
    ------------------------------------------------------------

    DECLARE @SaldoCartera DECIMAL(18,2) = 0;

    -- Cartera que NO está en mora.
    DECLARE @SaldoCreditos DECIMAL(18,2) = 0;

    -- Cartera correspondiente a créditos con mora.
    DECLARE @SaldoMoraCartera DECIMAL(18,2) = 0;

    -- Créditos con mora y atraso <= 90 días.
    DECLARE @SaldoVencido DECIMAL(18,2) = 0;

    -- Créditos con mora y atraso > 90 días.
    DECLARE @SaldoMorosidad DECIMAL(18,2) = 0;

    DECLARE @ClientesMora INT = 0;
    DECLARE @CreditosPorVencerSemana INT = 0;


    ------------------------------------------------------------
    -- TOTAL ANALISTAS ACTIVOS
    ------------------------------------------------------------

    SELECT
        @TotalAnalistas =
            COUNT(DISTINCT UR.UsuarioId)

    FROM MAESTRO.UsuarioRol UR

    INNER JOIN MAESTRO.Usuario U
        ON U.UsuarioId = UR.UsuarioId

    WHERE
        UR.RolId = 6
        AND U.Estado = 1
        AND U.NombreUsuario <> 'IRRECUPERABLE';

    ------------------------------------------------------------
    -- CRÉDITO: RESUMEN EN UNA SOLA LECTURA
    --
    -- Antes el SP recorría CREDITO.Credito varias veces para:
    --   * clientes
    --   * colocaciones
    --   * desembolsos
    --   * próximos vencimientos
    --
    -- Ahora estas métricas se calculan en una sola agregación.
    ------------------------------------------------------------

    SELECT
        @TotalClientes =
            COUNT(
                DISTINCT
                CASE
                    WHEN C.Estado = 'DES'
                     AND ISNULL(C.IndIrrecuperable, 0) = 0
                    THEN C.PersonaId
                END
            ),

        @CreditosHoy =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @Hoy
                 AND C.FechaDesembolso < @Manana
                THEN 1 ELSE 0 END), 0),

        @CreditosAyer =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @Ayer
                 AND C.FechaDesembolso < @Hoy
                THEN 1 ELSE 0 END), 0),

        @CreditosAnteayer =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @Anteayer
                 AND C.FechaDesembolso < @Ayer
                THEN 1 ELSE 0 END), 0),

        @CreditosMesActual =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @InicioMesActual
                 AND C.FechaDesembolso < @Manana
                THEN 1 ELSE 0 END), 0),

        @CreditosMesAnteriorComparable =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @InicioMesAnterior
                 AND C.FechaDesembolso < @FinMesAnteriorComparable
                THEN 1 ELSE 0 END), 0),

        @DesembolsoHoy =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @Hoy
                 AND C.FechaDesembolso < @Manana
                THEN ISNULL(C.MontoDesembolso, 0) ELSE 0 END), 0),

        @DesembolsoAyer =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @Ayer
                 AND C.FechaDesembolso < @Hoy
                THEN ISNULL(C.MontoDesembolso, 0) ELSE 0 END), 0),

        @DesembolsoAnteayer =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @Anteayer
                 AND C.FechaDesembolso < @Ayer
                THEN ISNULL(C.MontoDesembolso, 0) ELSE 0 END), 0),

        @DesembolsoMesActual =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @InicioMesActual
                 AND C.FechaDesembolso < @Manana
                THEN ISNULL(C.MontoDesembolso, 0) ELSE 0 END), 0),

        @DesembolsoMesAnteriorComparable =
            ISNULL(SUM(CASE
                WHEN C.Estado IN ('DES', 'PAG', 'REP')
                 AND C.FechaDesembolso >= @InicioMesAnterior
                 AND C.FechaDesembolso < @FinMesAnteriorComparable
                THEN ISNULL(C.MontoDesembolso, 0) ELSE 0 END), 0),

        @CreditosPorVencerSemana =
            ISNULL(SUM(CASE
                WHEN C.Estado = 'DES'
                 AND ISNULL(C.IndIrrecuperable, 0) = 0
                 AND C.FechaVencimiento >= @Hoy
                 AND C.FechaVencimiento < DATEADD(DAY, 8, @Hoy)
                THEN 1 ELSE 0 END), 0)

    FROM CREDITO.Credito C;


    ------------------------------------------------------------
    -- MOVIMIENTO CAJA: RESUMEN EN UNA SOLA LECTURA ACOTADA
    --
    -- Antes se recorría MovimientoCaja una vez para cobranza y
    -- otra vez para flujo, además sin un límite inferior de fecha.
    --
    -- El dato más antiguo necesario es el inicio del mes anterior,
    -- por lo que limitamos la lectura a ese período y calculamos
    -- cobranza + flujo en una sola agregación.
    ------------------------------------------------------------

    SELECT
        @CobradoHoy =
            ISNULL(SUM(CASE
                WHEN M.Operacion = 'CUO'
                 AND M.FechaReg >= @Hoy
                 AND M.FechaReg < @Manana
                THEN M.ImportePago ELSE 0 END), 0),

        @CobradoAyer =
            ISNULL(SUM(CASE
                WHEN M.Operacion = 'CUO'
                 AND M.FechaReg >= @Ayer
                 AND M.FechaReg < @Hoy
                THEN M.ImportePago ELSE 0 END), 0),

        @CobradoAnteayer =
            ISNULL(SUM(CASE
                WHEN M.Operacion = 'CUO'
                 AND M.FechaReg >= @Anteayer
                 AND M.FechaReg < @Ayer
                THEN M.ImportePago ELSE 0 END), 0),

        @CobradoMesActual =
            ISNULL(SUM(CASE
                WHEN M.Operacion = 'CUO'
                 AND M.FechaReg >= @InicioMesActual
                 AND M.FechaReg < @Manana
                THEN M.ImportePago ELSE 0 END), 0),

        @CobradoMesAnteriorComparable =
            ISNULL(SUM(CASE
                WHEN M.Operacion = 'CUO'
                 AND M.FechaReg >= @InicioMesAnterior
                 AND M.FechaReg < @FinMesAnteriorComparable
                THEN M.ImportePago ELSE 0 END), 0),

        @EntradasHoy =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @Hoy
                 AND M.FechaReg < @Manana
                 AND M.IndEntrada = 1
                THEN M.ImportePago ELSE 0 END), 0),

        @SalidasHoy =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @Hoy
                 AND M.FechaReg < @Manana
                 AND M.IndEntrada = 0
                THEN M.ImportePago ELSE 0 END), 0),

        @EntradasAyer =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @Ayer
                 AND M.FechaReg < @Hoy
                 AND M.IndEntrada = 1
                THEN M.ImportePago ELSE 0 END), 0),

        @SalidasAyer =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @Ayer
                 AND M.FechaReg < @Hoy
                 AND M.IndEntrada = 0
                THEN M.ImportePago ELSE 0 END), 0),

        @EntradasAnteayer =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @Anteayer
                 AND M.FechaReg < @Ayer
                 AND M.IndEntrada = 1
                THEN M.ImportePago ELSE 0 END), 0),

        @SalidasAnteayer =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @Anteayer
                 AND M.FechaReg < @Ayer
                 AND M.IndEntrada = 0
                THEN M.ImportePago ELSE 0 END), 0),

        @EntradasMesActual =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @InicioMesActual
                 AND M.FechaReg < @Manana
                 AND M.IndEntrada = 1
                THEN M.ImportePago ELSE 0 END), 0),

        @SalidasMesActual =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @InicioMesActual
                 AND M.FechaReg < @Manana
                 AND M.IndEntrada = 0
                THEN M.ImportePago ELSE 0 END), 0),

        @EntradasMesAnteriorComparable =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @InicioMesAnterior
                 AND M.FechaReg < @FinMesAnteriorComparable
                 AND M.IndEntrada = 1
                THEN M.ImportePago ELSE 0 END), 0),

        @SalidasMesAnteriorComparable =
            ISNULL(SUM(CASE
                WHEN M.FechaReg >= @InicioMesAnterior
                 AND M.FechaReg < @FinMesAnteriorComparable
                 AND M.IndEntrada = 0
                THEN M.ImportePago ELSE 0 END), 0)

    FROM CREDITO.MovimientoCaja M

    WHERE
        M.Estado = 1
        AND M.FechaReg >= @InicioMesAnterior
        AND M.FechaReg < @Manana;


    -- CARTERA GENERAL
    ------------------------------------------------------------
    --
    -- Se reemplaza el cálculo antiguo mediante OUTER APPLY.
    --
    -- La clasificación replica la semántica de
    -- CREDITO.usp_ListarSaldoCartera:
    --
    -- SaldoCartera:
    --     Saldo pendiente total.
    --
    -- SaldoMoraCartera:
    --     Saldo pendiente de créditos cuya mora calculada > 0.
    --
    -- SaldoVencido:
    --     Saldo con mora y atraso <= 90 días.
    --
    -- SaldoMorosidad:
    --     Saldo con mora y atraso > 90 días.
    ------------------------------------------------------------

    ;WITH CreditosCartera AS
    (
        SELECT
            C.CreditoId,
            C.PersonaId,
            C.FechaVencimiento

        FROM CREDITO.Credito C

        WHERE
            C.Estado = 'DES'

            /*
                Para pruebas históricas impedimos incorporar
                créditos desembolsados después de @FechaCorte.
            */
            AND C.FechaDesembolso < @Manana
    ),

    PlanPagoAgregado AS
    (
        SELECT
            P.CreditoId,

            SUM(
                ISNULL(P.Cuota, 0)
                +
                ISNULL(P.Cargo, 0)
            ) AS MontoTotal

        FROM CREDITO.PlanPago P

        INNER JOIN CreditosCartera C
            ON C.CreditoId = P.CreditoId

        GROUP BY
            P.CreditoId
    ),

    MovimientoCajaAgregado AS
    (
        SELECT
            M.CreditoId,

            SUM(M.ImportePago) AS MovCaja

        FROM CREDITO.MovimientoCaja M

        INNER JOIN CreditosCartera C
            ON C.CreditoId = M.CreditoId

        WHERE
            M.ImportePago > 0
            AND M.Operacion = 'CUO'
            AND M.Estado = 1

            /*
                Fundamental para que @FechaCorte sea reproducible.
            */
            AND M.FechaReg < @Manana

        GROUP BY
            M.CreditoId
    ),

    CarteraBase AS
    (
        SELECT
            C.CreditoId,
            C.PersonaId,

            CAST(
                ISNULL(PP.MontoTotal, 0)
                -
                ISNULL(MC.MovCaja, 0)

                AS DECIMAL(18,2)
            ) AS Saldo,

            /*
                Cálculo set-based equivalente al comportamiento
                utilizado por la función legacy de días de atraso,
                pero sin recorrer fecha por fecha con WHILE.
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

        FROM CreditosCartera C

        LEFT JOIN PlanPagoAgregado PP
            ON PP.CreditoId = C.CreditoId

        LEFT JOIN MovimientoCajaAgregado MC
            ON MC.CreditoId = C.CreditoId
    )

    SELECT

        --------------------------------------------------------
        -- CARTERA TOTAL
        --------------------------------------------------------

        @SaldoCartera =
            ISNULL(
                SUM(CB.Saldo),
                0
            ),


        --------------------------------------------------------
        -- CARTERA EN MORA
        --------------------------------------------------------

        @SaldoMoraCartera =
            ISNULL(
                SUM(
                    CASE
                        WHEN CB.Saldo > 0
                         AND CB.DiasAtrazoMora > 0
                        THEN CB.Saldo
                        ELSE 0
                    END
                ),
                0
            ),


        --------------------------------------------------------
        -- VENCIDO <= 90 DÍAS
        --------------------------------------------------------

        @SaldoVencido =
            ISNULL(
                SUM(
                    CASE
                        WHEN CB.Saldo > 0
                         AND CB.DiasAtrazoMora > 0
                         AND CB.DiasAtrazoMora <= 90
                        THEN CB.Saldo
                        ELSE 0
                    END
                ),
                0
            ),


        --------------------------------------------------------
        -- MOROSIDAD > 90 DÍAS
        --------------------------------------------------------

        @SaldoMorosidad =
            ISNULL(
                SUM(
                    CASE
                        WHEN CB.Saldo > 0
                         AND CB.DiasAtrazoMora > 0
                         AND CB.DiasAtrazoMora > 90
                        THEN CB.Saldo
                        ELSE 0
                    END
                ),
                0
            ),


        --------------------------------------------------------
        -- CLIENTES CON CARTERA EN MORA
        --------------------------------------------------------

        @ClientesMora =
            COUNT(
                DISTINCT
                CASE
                    WHEN CB.Saldo > 0
                         AND CB.DiasAtrazoMora > 0
                    THEN CB.PersonaId
                END
            )

    FROM CarteraBase CB;


    ------------------------------------------------------------
    -- CRÉDITOS SIN MORA
    ------------------------------------------------------------

    SET @SaldoCreditos =
        @SaldoCartera
        -
        @SaldoMoraCartera;

    ------------------------------------------------------------
    -- RESULTADO
    ------------------------------------------------------------

    SELECT

        --------------------------------------------------------
        -- GENERAL
        --------------------------------------------------------

        @TotalAnalistas
            AS TotalAnalistas,

        @TotalClientes
            AS TotalClientes,


        --------------------------------------------------------
        -- COLOCACIONES
        --------------------------------------------------------

        @CreditosHoy
            AS CreditosHoy,

        @CreditosAyer
            AS CreditosAyer,

        @CreditosAnteayer
            AS CreditosAnteayer,

        @CreditosMesActual
            AS CreditosMesActual,

        @CreditosMesAnteriorComparable
            AS CreditosMesAnteriorComparable,


        --------------------------------------------------------
        -- DESEMBOLSOS
        --------------------------------------------------------

        @DesembolsoHoy
            AS DesembolsoHoy,

        @DesembolsoAyer
            AS DesembolsoAyer,

        @DesembolsoAnteayer
            AS DesembolsoAnteayer,

        @DesembolsoMesActual
            AS DesembolsoMesActual,

        @DesembolsoMesAnteriorComparable
            AS DesembolsoMesAnteriorComparable,


        --------------------------------------------------------
        -- COBRANZA
        --------------------------------------------------------

        @CobradoHoy
            AS CobradoHoy,

        @CobradoAyer
            AS CobradoAyer,

        @CobradoAnteayer
            AS CobradoAnteayer,

        @CobradoMesActual
            AS CobradoMesActual,

        @CobradoMesAnteriorComparable
            AS CobradoMesAnteriorComparable,


        --------------------------------------------------------
        -- FLUJO DEL DÍA
        --------------------------------------------------------

        @EntradasHoy
            AS EntradasHoy,

        @SalidasHoy
            AS SalidasHoy,

        (
            @EntradasHoy
            -
            @SalidasHoy
        ) AS FlujoNetoHoy,


        --------------------------------------------------------
        -- FLUJO DE AYER
        --------------------------------------------------------

        @EntradasAyer
            AS EntradasAyer,

        @SalidasAyer
            AS SalidasAyer,

        (
            @EntradasAyer
            -
            @SalidasAyer
        ) AS FlujoNetoAyer,

        --------------------------------------------------------
        -- FLUJO DE ANTEAYER
        --------------------------------------------------------

        @EntradasAnteayer
            AS EntradasAnteayer,

        @SalidasAnteayer
            AS SalidasAnteayer,

        (
            @EntradasAnteayer
            -
            @SalidasAnteayer
        )
            AS FlujoNetoAnteayer,

        --------------------------------------------------------
        -- FLUJO MES ACTUAL
        --------------------------------------------------------

        @EntradasMesActual
            AS EntradasMesActual,

        @SalidasMesActual
            AS SalidasMesActual,

        (
            @EntradasMesActual
            -
            @SalidasMesActual
        ) AS FlujoNetoMesActual,


        --------------------------------------------------------
        -- FLUJO PERÍODO ANTERIOR COMPARABLE
        --------------------------------------------------------

        @EntradasMesAnteriorComparable
            AS EntradasMesAnteriorComparable,

        @SalidasMesAnteriorComparable
            AS SalidasMesAnteriorComparable,

        (
            @EntradasMesAnteriorComparable
            -
            @SalidasMesAnteriorComparable
        ) AS FlujoNetoMesAnteriorComparable,


        --------------------------------------------------------
        -- DESGLOSE DE CARTERA
        --------------------------------------------------------

        @SaldoCartera
            AS SaldoCartera,

        @SaldoCreditos
            AS SaldoCreditos,

        @SaldoMoraCartera
            AS SaldoMoraCartera,

        @SaldoVencido
            AS SaldoVencido,

        @SaldoMorosidad
            AS SaldoMorosidad,


        --------------------------------------------------------
        -- SITUACIÓN OPERACIONAL
        --------------------------------------------------------

        @ClientesMora
            AS ClientesMora,

        @CreditosPorVencerSemana
            AS CreditosPorVencerSemana,


        --------------------------------------------------------
        -- FECHA
        --------------------------------------------------------

        @Hoy
            AS FechaConsulta;

END;

