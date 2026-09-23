
CREATE   PROCEDURE CREDITO.usp_ObtenerAvanceMetasGerenciales
    @Periodo DATE = NULL,
    @OficinaId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ahora DATETIME2(0) = CREDITO.ufn_FechaGerencial();
    DECLARE @PeriodoActual DATE = DATEFROMPARTS(YEAR(@Ahora), MONTH(@Ahora), 1);
    DECLARE @PeriodoNormalizado DATE =
        CASE WHEN @Periodo IS NULL THEN @PeriodoActual
             ELSE DATEFROMPARTS(YEAR(@Periodo), MONTH(@Periodo), 1) END;
    DECLARE @FechaCorte DATE =
        CASE WHEN @PeriodoNormalizado = @PeriodoActual
             THEN CAST(@Ahora AS DATE)
             ELSE EOMONTH(@PeriodoNormalizado) END;

    CREATE TABLE #Avance
    (
        Orden SMALLINT NULL,
        UsuarioId INT NOT NULL,
        NombreUsuario VARCHAR(100) NULL,
        NombreCompleto VARCHAR(250) NULL,
        Asesor VARCHAR(150) NULL,
        Supervisor VARCHAR(150) NULL,
        Mercado VARCHAR(150) NULL,
        TipoCartera VARCHAR(20) NULL,
        Periodo DATE NOT NULL,
        CapitalBase DECIMAL(18,2) NULL,
        MetaCapitalCierre DECIMAL(18,2) NULL,
        CapitalActual DECIMAL(18,2) NOT NULL,
        DiferenciaCapital DECIMAL(18,2) NULL,
        CumplimientoCapitalPct DECIMAL(10,2) NULL,
        EstadoCapital VARCHAR(40) NULL,
        ClientesBase INT NULL,
        MetaClientesActivosCierre INT NULL,
        ClientesActivosActual INT NOT NULL,
        DiferenciaClientes INT NULL,
        CumplimientoClientesPct DECIMAL(10,2) NULL,
        EstadoClientes VARCHAR(40) NULL,
        VencidosBaseComparable DECIMAL(18,2) NULL,
        VencidosBaseLegacy DECIMAL(18,2) NULL,
        MetaVencidosMaximoCierre DECIMAL(18,2) NULL,
        VencidosActual DECIMAL(18,2) NOT NULL,
        MargenVencidos DECIMAL(18,2) NULL,
        EstadoVencidos VARCHAR(50) NULL,
        ClientesVencidosBase INT NULL,
        ClientesVencidosActual INT NOT NULL,
        ClientesVencidosPct DECIMAL(10,2) NOT NULL,
        MetaRecuperacionVencidosMes DECIMAL(18,2) NULL,
        RecuperacionVencidosActual DECIMAL(18,2) NULL,
        EstadoRecuperacion VARCHAR(50) NULL,
        MetaConfigurada BIT NOT NULL,
        FechaCalculo DATETIME2(0) NOT NULL,
        AvanceNoOficial BIT NOT NULL,
        ClientesNuevosMes INT NOT NULL
    );

    INSERT INTO #Avance
    EXEC CREDITO.usp_ObtenerAvanceMetasGerencialesBase
        @Periodo = @PeriodoNormalizado,
        @OficinaId = @OficinaId;

    IF @PeriodoNormalizado < @PeriodoActual
    BEGIN
        SELECT B.*,
               CAST(ISNULL(D.MontoClientesNuevosMes, 0) AS DECIMAL(18,2))
                   AS MontoClientesNuevosMes,
               CAST(ISNULL(D.MontoCobradoMes, 0) AS DECIMAL(18,2)) AS MontoCobradoMes,
               CAST(ISNULL(D.DesembolsosMes, 0) AS DECIMAL(18,2)) AS DesembolsosMes,
               ISNULL(D.NroOperacionesMes, 0) AS NroOperacionesMes
        FROM #Avance B
        LEFT JOIN CREDITO.CierreGerencial C
            ON C.Periodo = @PeriodoNormalizado AND C.Estado = 'CER'
        LEFT JOIN CREDITO.CierreGerencialDetalle D
            ON D.CierreGerencialId = C.CierreGerencialId
           AND D.UsuarioId = B.UsuarioId
        ORDER BY B.Orden;
        RETURN;
    END;

    SELECT B.*,
           CAST(ISNULL(A.MontoClientesNuevosMes, 0) AS DECIMAL(18,2))
               AS MontoClientesNuevosMes,
           CAST(ISNULL(A.MontoCobradoMes, 0) AS DECIMAL(18,2)) AS MontoCobradoMes,
           CAST(ISNULL(A.DesembolsosMes, 0) AS DECIMAL(18,2)) AS DesembolsosMes,
           ISNULL(A.NroOperacionesMes, 0) AS NroOperacionesMes
    FROM #Avance B
    LEFT JOIN CREDITO.ufn_ActividadGerencialMensual(
        @PeriodoNormalizado, @FechaCorte, @OficinaId) A
        ON A.UsuarioId = B.UsuarioId
    ORDER BY B.Orden;
END;

