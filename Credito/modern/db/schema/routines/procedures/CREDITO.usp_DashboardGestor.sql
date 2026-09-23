
CREATE PROCEDURE [CREDITO].[usp_DashboardGestor]
(
    @UsuarioId INT,
    @OficinaId INT,
    @FechaCorte DATE = NULL
)
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Hoy DATE = ISNULL(@FechaCorte, dbo.ufnFecha());
    DECLARE @Manana DATE = DATEADD(DAY, 1, @Hoy);
    DECLARE @Ayer DATE = DATEADD(DAY, -1, @Hoy);
    DECLARE @InicioMesActual DATE = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);
    DECLARE @InicioMesAnterior DATE = DATEADD(MONTH, -1, @InicioMesActual);
    DECLARE @InicioMesSiguiente DATE = DATEADD(MONTH, 1, @InicioMesActual);

    DECLARE @CreditosActual INT = 0;
    DECLARE @CreditosAnterior INT = 0;
    DECLARE @MontoActual DECIMAL(18,2) = 0;
    DECLARE @MontoAnterior DECIMAL(18,2) = 0;
    DECLARE @CobradoHoy DECIMAL(18,2) = 0;
    DECLARE @CobradoAyer DECIMAL(18,2) = 0;
    DECLARE @CobradoActual DECIMAL(18,2) = 0;
    DECLARE @CobradoAnterior DECIMAL(18,2) = 0;
    DECLARE @SaldoActual DECIMAL(18,2) = 0;
    DECLARE @MontoMora DECIMAL(18,2) = 0;
    DECLARE @ClientesMora INT = 0;
    DECLARE @ClientesMoraSinPago INT = 0;
    DECLARE @ClientesMoraNuncaPagaron INT = 0;
    DECLARE @ClientesMoraDejaronPagar INT = 0;
    DECLARE @ClientesMoraPagandoConAtraso INT = 0;
    DECLARE @PorVencerSemana INT = 0;
    DECLARE @TotalClientes INT = 0;
    DECLARE @ClientesNuevosActual INT = 0;
    DECLARE @ClientesNuevosAnterior INT = 0;

    /* Materializamos una sola vez la cartera del analista. */
    CREATE TABLE #Creditos
    (
        CreditoId INT NOT NULL,
        PersonaId INT NULL,
        Estado VARCHAR(3) NULL,
        FechaDesembolso DATETIME NULL,
        FechaVencimiento DATETIME NULL,
        MontoDesembolso DECIMAL(18,2) NOT NULL,
        IndIrrecuperable BIT NOT NULL
    );

    INSERT INTO #Creditos
    (
        CreditoId,
        PersonaId,
        Estado,
        FechaDesembolso,
        FechaVencimiento,
        MontoDesembolso,
        IndIrrecuperable
    )
    SELECT
        C.CreditoId,
        C.PersonaId,
        C.Estado,
        C.FechaDesembolso,
        C.FechaVencimiento,
        CAST(ISNULL(C.MontoDesembolso, 0) AS DECIMAL(18,2)) AS MontoDesembolso,
        ISNULL(C.IndIrrecuperable, 0) AS IndIrrecuperable
    FROM CREDITO.Credito C
    WHERE C.UsuarioRegId = @UsuarioId
      AND C.OficinaId = @OficinaId
      AND C.FechaDesembolso < @Manana;

    /*
       A diferencia de una variable de tabla, #Creditos genera estadísticas.
       Esto permite estimar correctamente analistas con carteras muy distintas.
    */
    CREATE UNIQUE CLUSTERED INDEX IX_Creditos_CreditoId
        ON #Creditos (CreditoId);

    CREATE NONCLUSTERED INDEX IX_Creditos_EstadoPersona
        ON #Creditos (Estado, IndIrrecuperable, PersonaId)
        INCLUDE (FechaDesembolso, FechaVencimiento, MontoDesembolso);

    /* Colocaciones, desembolso, clientes y próximos vencimientos: una lectura. */
    SELECT
        @CreditosActual = ISNULL(SUM(CASE
            WHEN Estado IN ('DES','PAG','REP')
             AND FechaDesembolso >= @InicioMesActual
             AND FechaDesembolso < @InicioMesSiguiente THEN 1 ELSE 0 END), 0),
        @CreditosAnterior = ISNULL(SUM(CASE
            WHEN Estado IN ('DES','PAG','REP')
             AND FechaDesembolso >= @InicioMesAnterior
             AND FechaDesembolso < @InicioMesActual THEN 1 ELSE 0 END), 0),
        @MontoActual = ISNULL(SUM(CASE
            WHEN Estado IN ('DES','PAG','REP')
             AND FechaDesembolso >= @InicioMesActual
             AND FechaDesembolso < @InicioMesSiguiente THEN MontoDesembolso ELSE 0 END), 0),
        @MontoAnterior = ISNULL(SUM(CASE
            WHEN Estado IN ('DES','PAG','REP')
             AND FechaDesembolso >= @InicioMesAnterior
             AND FechaDesembolso < @InicioMesActual THEN MontoDesembolso ELSE 0 END), 0),
        @PorVencerSemana = ISNULL(SUM(CASE
            WHEN Estado = 'DES'
             AND IndIrrecuperable = 0
             AND FechaVencimiento >= @Hoy
             AND FechaVencimiento < DATEADD(DAY, 8, @Hoy) THEN 1 ELSE 0 END), 0)
    FROM #Creditos;

    SELECT @TotalClientes = COUNT(DISTINCT PersonaId)
    FROM #Creditos
    WHERE Estado = 'DES'
      AND IndIrrecuperable = 0;

    /* Primera colocación válida de cada cliente con este analista/oficina. */
    ;WITH PrimeraColocacion AS
    (
        SELECT
            C.PersonaId,
            MIN(C.FechaDesembolso) AS PrimeraFechaDesembolso
        FROM CREDITO.Credito C
        WHERE C.UsuarioRegId = @UsuarioId
          AND C.OficinaId = @OficinaId
          AND C.PersonaId IS NOT NULL
          AND C.FechaDesembolso IS NOT NULL
          AND C.Estado IN ('DES','PAG','REP')
          AND C.FechaDesembolso < @Manana
        GROUP BY C.PersonaId
    )
    SELECT
        @ClientesNuevosActual = ISNULL(SUM(CASE
            WHEN PrimeraFechaDesembolso >= @InicioMesActual
             AND PrimeraFechaDesembolso < @InicioMesSiguiente THEN 1 ELSE 0 END), 0),
        @ClientesNuevosAnterior = ISNULL(SUM(CASE
            WHEN PrimeraFechaDesembolso >= @InicioMesAnterior
             AND PrimeraFechaDesembolso < @InicioMesActual THEN 1 ELSE 0 END), 0)
    FROM PrimeraColocacion;

    /* Cobranza diaria y mensual en una sola lectura acotada. */
    SELECT
        @CobradoHoy = ISNULL(SUM(CASE
            WHEN M.FechaReg >= @Hoy AND M.FechaReg < @Manana
            THEN M.ImportePago ELSE 0 END), 0),
        @CobradoAyer = ISNULL(SUM(CASE
            WHEN M.FechaReg >= @Ayer AND M.FechaReg < @Hoy
            THEN M.ImportePago ELSE 0 END), 0),
        @CobradoActual = ISNULL(SUM(CASE
            WHEN M.FechaReg >= @InicioMesActual AND M.FechaReg < @Manana
            THEN M.ImportePago ELSE 0 END), 0),
        @CobradoAnterior = ISNULL(SUM(CASE
            WHEN M.FechaReg >= @InicioMesAnterior AND M.FechaReg < @InicioMesActual
            THEN M.ImportePago ELSE 0 END), 0)
    FROM CREDITO.MovimientoCaja M
    INNER JOIN #Creditos C ON C.CreditoId = M.CreditoId
    WHERE M.Operacion = 'CUO'
      AND M.Estado = 1
      AND M.ImportePago > 0
      AND M.FechaReg >= @InicioMesAnterior
      AND M.FechaReg < @Manana;

    /*
       Mora real: existe al menos una cuota PEN vencida.
       MontoMora representa el saldo total pendiente de esos créditos,
       no únicamente la suma de cuotas vencidas.
    */
    ;WITH CreditosActivos AS
    (
        SELECT CreditoId, PersonaId
        FROM #Creditos
        WHERE Estado = 'DES'
          AND IndIrrecuperable = 0
    ),
    PlanAgregado AS
    (
        SELECT
            PP.CreditoId,
            SUM(ISNULL(PP.Cuota, 0) + ISNULL(PP.Cargo, 0)) AS MontoPlan,
            MAX(CASE
                WHEN PP.Estado = 'PEN'
                 AND PP.FechaVencimiento < @Hoy THEN 1 ELSE 0 END) AS TieneMora,
            MIN(CASE
                WHEN PP.Estado = 'PEN'
                 AND PP.FechaVencimiento < @Hoy THEN PP.FechaVencimiento END) AS PrimeraCuotaVencida
        FROM CREDITO.PlanPago PP
        INNER JOIN CreditosActivos CA ON CA.CreditoId = PP.CreditoId
        GROUP BY PP.CreditoId
    ),
    PagosAgregados AS
    (
        SELECT
            M.CreditoId,
            SUM(M.ImportePago) AS TotalPagado,
            MAX(M.FechaReg) AS FechaUltimoPago
        FROM CREDITO.MovimientoCaja M
        INNER JOIN CreditosActivos CA ON CA.CreditoId = M.CreditoId
        WHERE M.Operacion = 'CUO'
          AND M.Estado = 1
          AND M.ImportePago > 0
          AND M.FechaReg < @Manana
        GROUP BY M.CreditoId
    ),
    CarteraCredito AS
    (
        SELECT
            CA.CreditoId,
            CA.PersonaId,
            ISNULL(PA.TieneMora, 0) AS TieneMora,
            PA.PrimeraCuotaVencida,
            CAST(CASE
                WHEN ISNULL(PA.MontoPlan, 0) - ISNULL(PG.TotalPagado, 0) > 0
                THEN ISNULL(PA.MontoPlan, 0) - ISNULL(PG.TotalPagado, 0)
                ELSE 0 END AS DECIMAL(18,2)) AS Saldo,
            CAST(ISNULL(PG.TotalPagado, 0) AS DECIMAL(18,2)) AS TotalPagado,
            PG.FechaUltimoPago
        FROM CreditosActivos CA
        LEFT JOIN PlanAgregado PA ON PA.CreditoId = CA.CreditoId
        LEFT JOIN PagosAgregados PG ON PG.CreditoId = CA.CreditoId
    ),
    CarteraPersona AS
    (
        SELECT
            PersonaId,
            SUM(Saldo) AS SaldoTotal,
            SUM(CASE WHEN TieneMora = 1 THEN Saldo ELSE 0 END) AS SaldoMora,
            MAX(CASE WHEN TieneMora = 1 AND Saldo > 0 THEN 1 ELSE 0 END) AS TieneMora,
            SUM(CASE WHEN TieneMora = 1 THEN TotalPagado ELSE 0 END) AS PagadoEnCreditosMora,
            MAX(CASE
                WHEN TieneMora = 1
                 AND Saldo > 0
                 AND FechaUltimoPago >= PrimeraCuotaVencida THEN 1 ELSE 0 END) AS PagoDesdeInicioMora
        FROM CarteraCredito
        GROUP BY PersonaId
    )
    SELECT
        @SaldoActual = ISNULL(SUM(SaldoTotal), 0),
        @MontoMora = ISNULL(SUM(SaldoMora), 0),
        @ClientesMora = ISNULL(SUM(CASE WHEN TieneMora = 1 THEN 1 ELSE 0 END), 0),
        @ClientesMoraSinPago = ISNULL(SUM(CASE
            WHEN TieneMora = 1 AND PagoDesdeInicioMora = 0 THEN 1 ELSE 0 END), 0),
        @ClientesMoraNuncaPagaron = ISNULL(SUM(CASE
            WHEN TieneMora = 1 AND PagadoEnCreditosMora <= 0 THEN 1 ELSE 0 END), 0),
        @ClientesMoraDejaronPagar = ISNULL(SUM(CASE
            WHEN TieneMora = 1
             AND PagadoEnCreditosMora > 0
             AND PagoDesdeInicioMora = 0 THEN 1 ELSE 0 END), 0),
        @ClientesMoraPagandoConAtraso = ISNULL(SUM(CASE
            WHEN TieneMora = 1 AND PagoDesdeInicioMora = 1 THEN 1 ELSE 0 END), 0)
    FROM CarteraPersona;

    SELECT
        @CreditosActual AS CreditosActual,
        @CreditosAnterior AS CreditosAnterior,
        @MontoActual AS MontoActual,
        @MontoAnterior AS MontoAnterior,
        @CobradoHoy AS CobradoHoy,
        @CobradoAyer AS CobradoAyer,
        @CobradoActual AS CobradoActual,
        @CobradoAnterior AS CobradoAnterior,
        @SaldoActual AS SaldoActual,
        @MontoMora AS MontoMora,
        @TotalClientes AS TotalClientes,
        @ClientesNuevosActual AS ClientesNuevosActual,
        @ClientesNuevosAnterior AS ClientesNuevosAnterior,
        @ClientesMora AS ClientesMora,
        @ClientesMoraSinPago AS ClientesMoraSinPago,
        @ClientesMoraNuncaPagaron AS ClientesMoraNuncaPagaron,
        @ClientesMoraDejaronPagar AS ClientesMoraDejaronPagar,
        @ClientesMoraPagandoConAtraso AS ClientesMoraPagandoConAtraso,
        @PorVencerSemana AS PorVencerSemana,
        @Hoy AS FechaConsulta;
END

