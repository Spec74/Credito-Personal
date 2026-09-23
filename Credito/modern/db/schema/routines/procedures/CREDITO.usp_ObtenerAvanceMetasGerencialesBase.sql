
/* Consulta unificada: historico congelado o avance vivo del mes actual. */
CREATE   PROCEDURE CREDITO.usp_ObtenerAvanceMetasGerenciales
    @Periodo DATE=NULL,
    @OficinaId INT=1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @FechaCorte DATE=CAST(@Ahora AS DATE);
    DECLARE @PeriodoActual DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    SET @Periodo=CASE WHEN @Periodo IS NULL THEN @PeriodoActual
                      ELSE DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1) END;

    IF @Periodo>@PeriodoActual THROW 53030, 'No se puede consultar un periodo futuro.',1;

    IF @Periodo<@PeriodoActual
    BEGIN
        DECLARE @CierreId BIGINT,@FechaCierre DATETIME2(0);
        SELECT @CierreId=CierreGerencialId,@FechaCierre=FechaCierre
        FROM CREDITO.CierreGerencial WHERE Periodo=@Periodo AND Estado='CER';
        IF @CierreId IS NULL THROW 53031, 'No existe un cierre oficial para el periodo solicitado.',1;

        ;WITH Anterior AS
        (
            SELECT D.*
            FROM CREDITO.CierreGerencial C
            INNER JOIN CREDITO.CierreGerencialDetalle D
                ON D.CierreGerencialId=C.CierreGerencialId
            WHERE C.Periodo=DATEADD(MONTH,-1,@Periodo) AND C.Estado='CER'
        ),
        Datos AS
        (
            SELECT D.Orden,D.UsuarioId,D.NombreUsuario,D.NombreCompleto,
                   D.Asesor,D.Supervisor,D.Mercado,D.TipoCartera,
                   A.CapitalCierre AS CapitalBase,A.TotalClientesActivos AS ClientesBase,
                   CASE WHEN A.DefinicionVencidos=D.DefinicionVencidos
                        THEN COALESCE(A.VencidosCuotasCierre,A.MoraGeneral)
                        ELSE A.VencidosCuotasCierre END AS VencidosBaseComparable,
                   A.MoraGeneral AS VencidosBaseLegacy,
                   COALESCE(A.ClientesVencidosCierre,SP.NroClientesSaldoMoraCartera)
                        AS ClientesVencidosBase,
                   M.MetaCapitalCierre,M.MetaClientesActivosCierre,
                   M.MetaVencidosMaximoCierre,M.MetaRecuperacionVencidosMes,
                   D.CapitalCierre AS CapitalActual,
                   D.TotalClientesActivos AS ClientesActivosActual,
                   COALESCE(D.VencidosCuotasCierre,D.MoraGeneral) AS VencidosActual,
                   COALESCE(D.ClientesVencidosCierre,SA.NroClientesSaldoMoraCartera,0)
                        AS ClientesVencidosActual,
                   ISNULL(D.ClientesNuevosMes,0) AS ClientesNuevosMes
            FROM CREDITO.CierreGerencialDetalle D
            LEFT JOIN Anterior A ON A.UsuarioId=D.UsuarioId
            LEFT JOIN CREDITO.MetaGerencialAnalista M
                ON M.Periodo=@Periodo AND M.UsuarioId=D.UsuarioId AND M.Activo=1
            LEFT JOIN CREDITO.SaldoCarteraMensual SA
                ON SA.Anio=YEAR(@Periodo) AND SA.Mes=MONTH(@Periodo)
               AND SA.OficinaId=@OficinaId AND SA.AgenteId=D.UsuarioId
            LEFT JOIN CREDITO.SaldoCarteraMensual SP
                ON SP.Anio=YEAR(DATEADD(MONTH,-1,@Periodo))
               AND SP.Mes=MONTH(DATEADD(MONTH,-1,@Periodo))
               AND SP.OficinaId=@OficinaId AND SP.AgenteId=D.UsuarioId
            WHERE D.CierreGerencialId=@CierreId
        )
        SELECT D.Orden,D.UsuarioId,D.NombreUsuario,D.NombreCompleto,
               D.Asesor,D.Supervisor,D.Mercado,D.TipoCartera,@Periodo AS Periodo,
               CAST(D.CapitalBase AS DECIMAL(18,2)) AS CapitalBase,D.MetaCapitalCierre,
               CAST(D.CapitalActual AS DECIMAL(18,2)) AS CapitalActual,
               CAST(CASE WHEN D.MetaCapitalCierre IS NULL THEN NULL
                    ELSE D.CapitalActual-D.MetaCapitalCierre END AS DECIMAL(18,2)) AS DiferenciaCapital,
               CAST(CASE WHEN ISNULL(D.MetaCapitalCierre,0)=0 THEN NULL
                    ELSE D.CapitalActual*100.0/D.MetaCapitalCierre END AS DECIMAL(10,2)) AS CumplimientoCapitalPct,
               CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                    WHEN D.MetaCapitalCierre IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.CapitalActual>=D.MetaCapitalCierre THEN 'CUMPLIDA'
                    ELSE 'NO CUMPLIDA' END AS EstadoCapital,
               D.ClientesBase,D.MetaClientesActivosCierre,D.ClientesActivosActual,
               CASE WHEN D.MetaClientesActivosCierre IS NULL THEN NULL
                    ELSE D.ClientesActivosActual-D.MetaClientesActivosCierre END AS DiferenciaClientes,
               CAST(CASE WHEN ISNULL(D.MetaClientesActivosCierre,0)=0 THEN NULL
                    ELSE D.ClientesActivosActual*100.0/D.MetaClientesActivosCierre END AS DECIMAL(10,2)) AS CumplimientoClientesPct,
               CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                    WHEN D.MetaClientesActivosCierre IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.ClientesActivosActual>=D.MetaClientesActivosCierre THEN 'CUMPLIDA'
                    ELSE 'NO CUMPLIDA' END AS EstadoClientes,
               CAST(D.VencidosBaseComparable AS DECIMAL(18,2)) AS VencidosBaseComparable,
               CAST(D.VencidosBaseLegacy AS DECIMAL(18,2)) AS VencidosBaseLegacy,
               D.MetaVencidosMaximoCierre,CAST(D.VencidosActual AS DECIMAL(18,2)) AS VencidosActual,
               CAST(CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN NULL
                    ELSE D.MetaVencidosMaximoCierre-D.VencidosActual END AS DECIMAL(18,2)) AS MargenVencidos,
               CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.VencidosActual<=D.MetaVencidosMaximoCierre THEN 'DENTRO DEL LIMITE'
                    ELSE 'LIMITE EXCEDIDO' END AS EstadoVencidos,
               D.ClientesVencidosBase,D.ClientesVencidosActual,
               CAST(CASE WHEN D.ClientesActivosActual=0 THEN 0
                    ELSE D.ClientesVencidosActual*100.0/D.ClientesActivosActual END AS DECIMAL(10,2))
                    AS ClientesVencidosPct,
               D.MetaRecuperacionVencidosMes,
               CAST(NULL AS DECIMAL(18,2)) AS RecuperacionVencidosActual,
               CASE WHEN D.MetaRecuperacionVencidosMes IS NULL THEN 'SIN CONFIGURAR'
                    WHEN D.VencidosBaseComparable IS NULL THEN 'SIN BASE COMPARABLE'
                    ELSE 'NO DISPONIBLE EN HISTORICO LEGACY' END AS EstadoRecuperacion,
               CAST(CASE WHEN D.TipoCartera='ESPECIAL'
                              AND D.MetaVencidosMaximoCierre IS NOT NULL
                              AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                         WHEN D.TipoCartera='PRODUCTIVA'
                              AND D.MetaCapitalCierre IS NOT NULL
                              AND D.MetaClientesActivosCierre IS NOT NULL
                              AND D.MetaVencidosMaximoCierre IS NOT NULL
                              AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                         ELSE 0 END AS BIT) AS MetaConfigurada,
               @FechaCierre AS FechaCalculo,CAST(0 AS BIT) AS AvanceNoOficial,
               D.ClientesNuevosMes
        FROM Datos D ORDER BY D.Orden;
        RETURN;
    END;

    ;WITH Anterior AS
    (
        SELECT D.UsuarioId,D.CapitalCierre,D.TotalClientesActivos,
               D.MoraGeneral AS VencidosBaseLegacy,
               D.VencidosCuotasCierre AS VencidosBaseComparable,
               D.ClientesVencidosCierre
        FROM CREDITO.CierreGerencial C
        INNER JOIN CREDITO.CierreGerencialDetalle D ON D.CierreGerencialId=C.CierreGerencialId
        WHERE C.Periodo=DATEADD(MONTH,-1,@Periodo) AND C.Estado='CER'
    ),
    Datos AS
    (
        SELECT M.Orden,M.UsuarioId,CAST(U.NombreUsuario AS VARCHAR(100)) AS NombreUsuario,
               CAST(P.NombreCompleto AS VARCHAR(250)) AS NombreCompleto,
               M.Asesor,M.Supervisor,M.Mercado,M.TipoCartera,
               A.CapitalCierre AS CapitalBase,A.TotalClientesActivos AS ClientesBase,
               A.VencidosBaseComparable,A.VencidosBaseLegacy,
               A.ClientesVencidosCierre AS ClientesVencidosBase,
               M.MetaCapitalCierre,M.MetaClientesActivosCierre,
               M.MetaVencidosMaximoCierre,M.MetaRecuperacionVencidosMes,
               ISNULL(X.CapitalActual,0) AS CapitalActual,
               ISNULL(X.ClientesActivosActual,0) AS ClientesActivosActual,
               ISNULL(X.VencidosActual,0) AS VencidosActual,
               ISNULL(X.ClientesVencidosActual,0) AS ClientesVencidosActual,
               R.RecuperacionVencidosActual,ISNULL(N.ClientesNuevosMes,0) AS ClientesNuevosMes
        FROM CREDITO.MetaGerencialAnalista M
        INNER JOIN MAESTRO.Usuario U ON U.UsuarioId=M.UsuarioId
        LEFT JOIN MAESTRO.Persona P ON P.PersonaId=U.PersonaId
        LEFT JOIN Anterior A ON A.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_MetricasGerencialesActuales(@FechaCorte,@OficinaId) X
            ON X.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_RecuperacionVencidosGerencial(@Periodo,@FechaCorte) R
            ON R.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_ClientesNuevosGerenciales(@Periodo,@OficinaId) N
            ON N.UsuarioId=M.UsuarioId
        WHERE M.Periodo=@Periodo AND M.Activo=1
    )
    SELECT D.Orden,D.UsuarioId,D.NombreUsuario,D.NombreCompleto,
           D.Asesor,D.Supervisor,D.Mercado,D.TipoCartera,@Periodo AS Periodo,
           CAST(D.CapitalBase AS DECIMAL(18,2)) AS CapitalBase,D.MetaCapitalCierre,
           CAST(D.CapitalActual AS DECIMAL(18,2)) AS CapitalActual,
           CAST(CASE WHEN D.MetaCapitalCierre IS NULL THEN NULL
                ELSE D.CapitalActual-D.MetaCapitalCierre END AS DECIMAL(18,2)) AS DiferenciaCapital,
           CAST(CASE WHEN ISNULL(D.MetaCapitalCierre,0)=0 THEN NULL
                ELSE D.CapitalActual*100.0/D.MetaCapitalCierre END AS DECIMAL(10,2)) AS CumplimientoCapitalPct,
           CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                WHEN D.MetaCapitalCierre IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.CapitalActual>=D.MetaCapitalCierre THEN 'CUMPLIDA' ELSE 'PENDIENTE' END AS EstadoCapital,
           D.ClientesBase,D.MetaClientesActivosCierre,D.ClientesActivosActual,
           CASE WHEN D.MetaClientesActivosCierre IS NULL THEN NULL
                ELSE D.ClientesActivosActual-D.MetaClientesActivosCierre END AS DiferenciaClientes,
           CAST(CASE WHEN ISNULL(D.MetaClientesActivosCierre,0)=0 THEN NULL
                ELSE D.ClientesActivosActual*100.0/D.MetaClientesActivosCierre END AS DECIMAL(10,2)) AS CumplimientoClientesPct,
           CASE WHEN D.TipoCartera='ESPECIAL' THEN 'NO APLICA'
                WHEN D.MetaClientesActivosCierre IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.ClientesActivosActual>=D.MetaClientesActivosCierre THEN 'CUMPLIDA' ELSE 'PENDIENTE' END AS EstadoClientes,
           CAST(D.VencidosBaseComparable AS DECIMAL(18,2)) AS VencidosBaseComparable,
           CAST(D.VencidosBaseLegacy AS DECIMAL(18,2)) AS VencidosBaseLegacy,
           D.MetaVencidosMaximoCierre,CAST(D.VencidosActual AS DECIMAL(18,2)) AS VencidosActual,
           CAST(CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN NULL
                ELSE D.MetaVencidosMaximoCierre-D.VencidosActual END AS DECIMAL(18,2)) AS MargenVencidos,
           CASE WHEN D.MetaVencidosMaximoCierre IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.VencidosActual<=D.MetaVencidosMaximoCierre THEN 'DENTRO DEL LIMITE'
                ELSE 'LIMITE EXCEDIDO' END AS EstadoVencidos,
           D.ClientesVencidosBase,D.ClientesVencidosActual,
           CAST(CASE WHEN D.ClientesActivosActual=0 THEN 0
                ELSE D.ClientesVencidosActual*100.0/D.ClientesActivosActual END AS DECIMAL(10,2))
                AS ClientesVencidosPct,
           D.MetaRecuperacionVencidosMes,
           CAST(D.RecuperacionVencidosActual AS DECIMAL(18,2)) AS RecuperacionVencidosActual,
           CASE WHEN D.MetaRecuperacionVencidosMes IS NULL THEN 'SIN CONFIGURAR'
                WHEN D.VencidosBaseComparable IS NULL THEN 'SIN BASE COMPARABLE'
                WHEN D.RecuperacionVencidosActual>=D.MetaRecuperacionVencidosMes THEN 'CUMPLIDA'
                ELSE 'PENDIENTE' END AS EstadoRecuperacion,
           CAST(CASE WHEN D.TipoCartera='ESPECIAL'
                          AND D.MetaVencidosMaximoCierre IS NOT NULL
                          AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     WHEN D.TipoCartera='PRODUCTIVA'
                          AND D.MetaCapitalCierre IS NOT NULL
                          AND D.MetaClientesActivosCierre IS NOT NULL
                          AND D.MetaVencidosMaximoCierre IS NOT NULL
                          AND D.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     ELSE 0 END AS BIT) AS MetaConfigurada,
           @Ahora AS FechaCalculo,CAST(1 AS BIT) AS AvanceNoOficial,D.ClientesNuevosMes
    FROM Datos D ORDER BY D.Orden;
END;

