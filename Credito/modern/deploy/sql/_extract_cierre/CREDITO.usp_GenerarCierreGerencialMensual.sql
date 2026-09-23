
CREATE   PROCEDURE CREDITO.usp_GenerarCierreGerencialMensual
    @Periodo DATE,
    @OficinaId INT,
    @UsuarioCierreId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Periodo IS NULL OR @OficinaId IS NULL OR @UsuarioCierreId IS NULL
        THROW 53040, 'Periodo, oficina y usuario de cierre son obligatorios.',1;

    SET @Periodo=DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1);
    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @FechaHoy DATE=CAST(@Ahora AS DATE);
    DECLARE @PeriodoActual DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @PeriodoAnterior DATE=DATEADD(MONTH,-1,@PeriodoActual);
    DECLARE @FechaCorte DATE=EOMONTH(@Periodo);
    DECLARE @PeriodoSiguiente DATE=DATEADD(MONTH,1,@Periodo);
    DECLARE @CierreId BIGINT,@Bloqueo INT;

    IF @Periodo<>@PeriodoActual AND @Periodo<>@PeriodoAnterior
        THROW 53041, 'Solo puede cerrarse el periodo actual o el inmediatamente anterior.',1;
    IF @Periodo=@PeriodoActual AND @FechaHoy<>@FechaCorte
        THROW 53042, 'El periodo actual solo puede cerrarse en su ultimo dia.',1;
    IF @Periodo=@PeriodoAnterior AND DAY(@FechaHoy)>2
        THROW 53043, 'La contingencia para el mes anterior termina el dia 2.',1;
    IF (SELECT COUNT(*) FROM CREDITO.MetaGerencialAnalista
        WHERE Periodo=@Periodo AND Activo=1)<>19
        THROW 53044, 'El periodo no contiene las 19 carteras esperadas.',1;
    IF (SELECT COUNT(*) FROM CREDITO.MetaGerencialAnalista
        WHERE Periodo=@Periodo AND Activo=1 AND TipoCartera='PRODUCTIVA')<>16
        THROW 53045, 'El periodo no contiene las 16 carteras productivas.',1;
    IF (SELECT COUNT(*) FROM CREDITO.MetaGerencialAnalista
        WHERE Periodo=@Periodo AND Activo=1 AND TipoCartera='ESPECIAL')<>3
        THROW 53046, 'El periodo no contiene las 3 carteras especiales.',1;
    IF EXISTS
    (
        SELECT 1 FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo=@Periodo AND M.Activo=1
          AND (M.MetaVencidosMaximoCierre IS NULL OR M.MetaRecuperacionVencidosMes IS NULL
               OR (M.TipoCartera='PRODUCTIVA' AND
                   (M.MetaCapitalCierre IS NULL OR M.MetaClientesActivosCierre IS NULL)))
    )
        THROW 53047, 'No puede cerrarse el periodo porque faltan metas.',1;

    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC @Bloqueo=sys.sp_getapplock
            @Resource=N'CREDITO.CIERRE_GERENCIAL_MENSUAL',@LockMode=N'Exclusive',
            @LockOwner=N'Transaction',@LockTimeout=10000;
        IF @Bloqueo<0 THROW 53048, 'No se pudo obtener el bloqueo exclusivo del cierre.',1;

        SELECT @CierreId=CierreGerencialId
        FROM CREDITO.CierreGerencial WITH(UPDLOCK,HOLDLOCK)
        WHERE Periodo=@Periodo AND Estado='CER';
        IF @CierreId IS NOT NULL
        BEGIN
            COMMIT TRANSACTION;
            SELECT @CierreId AS CierreGerencialId,@Periodo AS Periodo,CAST(0 AS BIT) AS Creado,
                   'EL CIERRE YA EXISTIA; NO FUE MODIFICADO' AS Resultado;
            RETURN;
        END;

        INSERT INTO CREDITO.CierreGerencial
            (Periodo,FechaCorte,Estado,Version,UsuarioCierreId,FechaCierre,Observacion)
        VALUES(@Periodo,@FechaCorte,'CER',1,@UsuarioCierreId,@Ahora,
               'CIERRE OFICIAL: CUOTAS FIFO, PAGOS VALIDOS Y CLIENTES NUEVOS');
        SET @CierreId=SCOPE_IDENTITY();

        INSERT INTO CREDITO.CierreGerencialDetalle
        (
            CierreGerencialId,UsuarioId,NombreUsuario,NombreCompleto,Asesor,Supervisor,
            Mercado,Orden,TipoCartera,FuenteInicial,CapitalCierre,TotalClientesActivos,
            MoraGeneral,VencidosCuotasCierre,ClientesVencidosCierre,DefinicionVencidos,
            ClientesNuevosMes
        )
        SELECT @CierreId,M.UsuarioId,CAST(U.NombreUsuario AS VARCHAR(100)),
               CAST(P.NombreCompleto AS VARCHAR(250)),M.Asesor,M.Supervisor,M.Mercado,
               M.Orden,M.TipoCartera,'CREDITO.ufn_MetricasGerencialesActuales',
               A.CapitalActual,A.ClientesActivosActual,A.VencidosActual,A.VencidosActual,
               A.ClientesVencidosActual,'CUOTAS_FIFO_PAGOS_VALIDOS',
               ISNULL(N.ClientesNuevosMes,0)
        FROM CREDITO.MetaGerencialAnalista M
        INNER JOIN MAESTRO.Usuario U ON U.UsuarioId=M.UsuarioId
        LEFT JOIN MAESTRO.Persona P ON P.PersonaId=U.PersonaId
        INNER JOIN CREDITO.ufn_MetricasGerencialesActuales(@FechaCorte,@OficinaId) A
            ON A.UsuarioId=M.UsuarioId
        LEFT JOIN CREDITO.ufn_ClientesNuevosGerenciales(@Periodo,@OficinaId) N
            ON N.UsuarioId=M.UsuarioId
        WHERE M.Periodo=@Periodo AND M.Activo=1;
        IF @@ROWCOUNT<>19 THROW 53049, 'El detalle calculado no contiene las 19 carteras.',1;

        INSERT INTO CREDITO.VencidoGerencialAperturaDetalle
            (Periodo,UsuarioId,CreditoId,PlanPagoId,FechaVencimiento,
             SaldoVencidoApertura,CierreOrigenId,FechaRegistro)
        SELECT @PeriodoSiguiente,Q.UsuarioId,Q.CreditoId,Q.PlanPagoId,Q.FechaVencimiento,
               Q.SaldoCuota,@CierreId,@Ahora
        FROM CREDITO.ufn_SaldosCuotasGerenciales(@FechaCorte,@OficinaId) Q
        WHERE Q.FechaVencimiento<@FechaCorte AND Q.SaldoCuota>0;

        MERGE CREDITO.MetaGerencialAnalista WITH(HOLDLOCK) AS D
        USING
        (
            SELECT UsuarioId,Asesor,Supervisor,Mercado,Orden,TipoCartera
            FROM CREDITO.MetaGerencialAnalista WHERE Periodo=@Periodo AND Activo=1
        ) O
        ON D.Periodo=@PeriodoSiguiente AND D.UsuarioId=O.UsuarioId
        WHEN MATCHED THEN UPDATE SET
            Asesor=O.Asesor,Supervisor=O.Supervisor,Mercado=O.Mercado,Orden=O.Orden,
            TipoCartera=O.TipoCartera,FuenteInicial=CONCAT('CIERRE GERENCIAL ',@CierreId),
            Activo=1,UsuarioModificacionId=@UsuarioCierreId,FechaModificacion=@Ahora
        WHEN NOT MATCHED THEN INSERT
            (Periodo,UsuarioId,Asesor,Supervisor,Mercado,Orden,TipoCartera,FuenteInicial,
             MetaCapitalCierre,MetaClientesActivosCierre,MetaVencidosMaximoCierre,
             MetaRecuperacionVencidosMes,Activo,UsuarioRegistroId,FechaRegistro)
        VALUES(@PeriodoSiguiente,O.UsuarioId,O.Asesor,O.Supervisor,O.Mercado,O.Orden,
               O.TipoCartera,CONCAT('CIERRE GERENCIAL ',@CierreId),NULL,NULL,NULL,NULL,1,
               @UsuarioCierreId,@Ahora);

        COMMIT TRANSACTION;
        SELECT @CierreId AS CierreGerencialId,@Periodo AS Periodo,@FechaCorte AS FechaCorte,
               @PeriodoSiguiente AS PeriodoSiguiente,CAST(1 AS BIT) AS Creado,
               'CIERRE GENERADO CORRECTAMENTE' AS Resultado;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

