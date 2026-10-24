-- Cuadre multisucursal (2026-10-24)
-- 1) usp_TransferirBoveda: @TipoPagoId (default 1) en TRS/TRE; no overdraw del medio.
-- 2) usp_CerrarBoveda: una principal; no cerrar con cajas abiertas / no enviadas.
-- 3) usp_CerrarBovedaTemporal: todos los medios; UsuarioRol ENCARGADO por OficinaId.
-- 4) usp_CerrarCajasDiarios: ROUND 2 decimales (era 1).
-- 5) B-06: UNIQUE una principal abierta por oficina (tabla chica; no 1.6M).
-- No cambia usp_ActualizarSaldosBoveda ni flag 1.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_TransferirBoveda](
@BovedaInicioId INT,
@BovedaDestinoId INT,
@Glosa VARCHAR(MAX),
@Monto DECIMAL(16,2),
@UsuarioRegId INT,
@flagAceptar INT,
@BovedaMovTempId INT=0,
@TipoPagoId SMALLINT=1)
AS
DECLARE @Estado BIT,@FechaReg DATETIME
SET @Estado= 1
SET @FechaReg = dbo.ufnFecha()

IF ISNULL(@TipoPagoId, 0) < 1
	SET @TipoPagoId = 1

IF @flagAceptar = 0
	BEGIN
		IF @BovedaInicioId IS NULL OR @BovedaDestinoId IS NULL
			OR @BovedaInicioId < 1 OR @BovedaDestinoId < 1
		BEGIN
			RAISERROR(N'Origen y destino de bóveda son obligatorios.', 16, 1)
			RETURN
		END

		IF @BovedaInicioId = @BovedaDestinoId
		BEGIN
			RAISERROR(N'La bóveda de origen y destino no pueden ser la misma.', 16, 1)
			RETURN
		END

		IF @Monto IS NULL OR @Monto <= 0
		BEGIN
			RAISERROR(N'El monto debe ser mayor a cero.', 16, 1)
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId AND IndCierre = 0)
		BEGIN
			RAISERROR(N'La bóveda de origen no está abierta.', 16, 1)
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = @BovedaDestinoId AND IndCierre = 0)
		BEGIN
			RAISERROR(N'La bóveda de destino no está abierta.', 16, 1)
			RETURN
		END

		DECLARE @HayCuentas BIT = CASE
			WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaInicioId)
			THEN 1 ELSE 0 END
		DECLARE @SiBoveda DECIMAL(16,2) = (
			SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaInicioId)
		DECLARE @SiMedio DECIMAL(16,2) = ISNULL((
			SELECT SaldoInicial
			FROM CREDITO.BovedaCuenta
			WHERE BovedaId = @BovedaInicioId AND TipoPagoId = @TipoPagoId), 0)
		IF @HayCuentas = 0 AND @TipoPagoId = 1
			SET @SiMedio = ISNULL(@SiBoveda, 0)
		DECLARE @NetoMedio DECIMAL(16,2) = ISNULL((
			SELECT SUM(IIF(IndEntrada = 1, Importe, -Importe))
			FROM CREDITO.BovedaMov
			WHERE BovedaId = @BovedaInicioId AND Estado = 1 AND TipoPagoId = @TipoPagoId), 0)

		IF (@SiMedio + @NetoMedio) < @Monto
		BEGIN
			RAISERROR(N'Saldo insuficiente en el medio de origen.', 16, 1)
			RETURN
		END

		INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion,Glosa,Importe,IndEntrada,Estado,UsuarioRegId,FechaReg,TipoPagoId)
					VALUES(@BovedaInicioId, 'TRS',@Glosa, @Monto,0,@Estado,@UsuarioRegId,@FechaReg,@TipoPagoId)
		EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId

		INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion,Glosa,Importe,IndEntrada,Estado,UsuarioRegId,FechaReg,TipoPagoId)
		VALUES(@BovedaDestinoId, 'TRE',@Glosa, @Monto,1,@Estado,@UsuarioRegId,@FechaReg,@TipoPagoId)

		EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId
	END

IF @flagAceptar = 2
	BEGIN
	DELETE CREDITO.BovedaMov WHERE MovimientoBovedaId = (SELECT MovimientoBovedaIniId FROM CREDITO.BovedaMovTemp
														 WHERE BovedaMovTempId = @BovedaMovTempId )
	SET @BovedaInicioId = (SELECT BovedaInicioId  FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId)

	EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId

	DELETE CREDITO.BovedaMovTemp where BovedaMovTempId = @BovedaMovTempId
	END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CerrarBoveda]
    @OficinaId INT,
	@UsuarioRegId INT
AS
DECLARE @Principales INT = (
	SELECT COUNT(1)
	FROM CREDITO.Boveda
	WHERE OficinaId = @OficinaId AND IndTemporal = 0 AND IndCierre = 0)

IF @Principales = 0
BEGIN
	RAISERROR(N'No hay bóveda principal abierta en la oficina.', 16, 1)
	RETURN
END

IF @Principales > 1
BEGIN
	RAISERROR(N'Hay más de una bóveda principal abierta en la oficina. Corrija antes de cerrar.', 16, 1)
	RETURN
END

IF EXISTS (
	SELECT 1
	FROM CREDITO.CajaDiario AS cd
	INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
	WHERE cd.IndCierre = 0 AND c.OficinaId = @OficinaId)
BEGIN
	RAISERROR(N'EXISTEN CAJAS ABIERTAS.', 16, 1)
	RETURN
END

IF EXISTS (
	SELECT 1
	FROM CREDITO.CajaDiario AS cd
	INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
	WHERE cd.IndCierre = 1 AND cd.TransBoveda = 0 AND c.OficinaId = @OficinaId)
BEGIN
	RAISERROR(N'EXISTEN CAJAS CERRADAS NO ENVIADAS A BOVEDA. REVISE FORMULARIO SALDOS CAJA.', 16, 1)
	RETURN
END

DECLARE @BovedaId INT = (SELECT BovedaId FROM CREDITO.Boveda WHERE OficinaId=@OficinaId AND	IndTemporal=0 AND IndCierre=0)
DECLARE @BovedaNuevoId INT
EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId
DECLARE @SaldoFinal DECIMAL(15,2) = (SELECT SaldoFinal FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaId

INSERT INTO CREDITO.Boveda
(OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal, FechaIniOperacion, FechaFinOperacion, IndCierre, IndTemporal)
VALUES (@OficinaId,@SaldoFinal, 0, 0, @SaldoFinal, dbo.ufnFecha(), NULL,0, 0 )

SELECT @BovedaNuevoId=@@IDENTITY

	;WITH CUENTAS AS (
		SELECT MC.BovedaId,MC.TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
		FROM CREDITO.BovedaMov MC
		WHERE MC.BovedaId=@BovedaId
		AND MC.Estado=1
		GROUP BY MC.BovedaId,MC.TipoPagoId
	)
	INSERT	INTO CREDITO.BovedaCuenta
	(BovedaId,TipoPagoId,SaldoInicial)
	SELECT @BovedaNuevoId,vt.ItemId, ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0) 'SaldoFinal'
	FROM MAESTRO.ValorTabla vt
	LEFT JOIN CUENTAS C ON  vt.ItemId=c.TipoPagoId
	LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaId and BC.TipoPagoId = vt.ItemId
	WHERE vt.TablaId=13 AND vt.ItemId>0

	DELETE CREDITO.BovedaCuenta WHERE BovedaId=@BovedaNuevoId AND SaldoInicial=0
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CerrarBovedaTemporal]
    @OficinaId INT,
	@UsuarioRegId INT
AS
DECLARE @BovedaId INT, @BovedaIdTemporal INT, @ImporteBovedaTemporal DECIMAL(15,2), @RolEncargado INT

SELECT @BovedaIdTemporal=BovedaId, @ImporteBovedaTemporal=SaldoFinal
FROM CREDITO.Boveda
WHERE OficinaId=@OficinaId AND	IndTemporal=1 AND IndCierre=0

SELECT @BovedaId=BovedaId
FROM CREDITO.Boveda
WHERE OficinaId=@OficinaId AND	IndTemporal=0 AND IndCierre=0

IF @BovedaIdTemporal IS NULL
BEGIN
	RAISERROR(N'No hay bóveda temporal abierta en la oficina.', 16, 1)
	RETURN
END

IF @BovedaId IS NULL
BEGIN
	RAISERROR(N'No hay bóveda principal abierta para devolver el saldo temporal.', 16, 1)
	RETURN
END

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaIdTemporal

;WITH CUENTAS AS (
	SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
	FROM CREDITO.BovedaMov MC
	WHERE MC.BovedaId=@BovedaIdTemporal
	AND MC.Estado=1
	GROUP BY MC.BovedaId,MC.TipoPagoId
)
INSERT	INTO CREDITO.BovedaMov(BovedaId,CodOperacion,Glosa,TipoPagoId,Importe,IndEntrada, Estado, CajaDiarioId, UsuarioRegId, FechaReg)
SELECT @BovedaId,'TRE','CIERRE BOVEDA TEMPORAL',vt.ItemId, ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0),1,1,0,@UsuarioRegId,dbo.ufnFecha()
FROM MAESTRO.ValorTabla vt
LEFT JOIN CUENTAS C ON vt.ItemId = C.TipoPagoId
LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaIdTemporal AND BC.TipoPagoId = vt.ItemId
WHERE vt.TablaId=13 AND vt.ItemId>0
AND ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0) <> 0

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaIdTemporal

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId

SELECT @RolEncargado=r.RolId
FROM MAESTRO.Rol AS r
WHERE r.Denominacion ='ENCARGADO'

IF @RolEncargado IS NOT NULL
	DELETE FROM MAESTRO.UsuarioRol
	WHERE RolId=@RolEncargado AND OficinaId=@OficinaId
GO

CREATE OR ALTER PROC [CREDITO].[usp_CerrarCajasDiarios]
    @UsuarioCierreId INT,
    @OficinaId INT = 1,
    @Sobrante DECIMAL(15, 2) = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @FechaTexto VARCHAR(20) = 'CIERRE ' + CONVERT(varchar, @Fecha, 103) + ' ';

    DECLARE @Principales INT = (
        SELECT COUNT(1)
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );
    IF @Principales > 1
    BEGIN
        RAISERROR(N'Hay más de una bóveda principal abierta en la oficina.', 16, 1);
        RETURN;
    END

    DECLARE @BovedaId INT = (
        SELECT BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 0
    );

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = 0
          AND IndTemporal = 1
    )
        SET @BovedaId = (
            SELECT BovedaId
            FROM CREDITO.Boveda
            WHERE OficinaId = @OficinaId
              AND IndCierre = 0
              AND IndTemporal = 1
        );

    IF @BovedaId IS NULL
    BEGIN
        RAISERROR(
            N'No se pueden cerrar las cajas: no existe una bóveda activa abierta en la oficina.',
            16,
            1
        );
        RETURN;
    END;

    ;WITH MOVS_BASE AS (
        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            1 AS TipoPagoId,
            CD.SaldoInicial AS Importe
        FROM CREDITO.CajaDiario CD
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId

        UNION ALL

        SELECT
            C.Denominacion AS Caja,
            CD.CajaDiarioId,
            MC.TipoPagoId,
            SUM(IIF(MC.IndEntrada = 1, MC.ImportePago, -MC.ImportePago)) AS Importe
        FROM CREDITO.MovimientoCaja MC
        INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = MC.CajaDiarioId
        INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
        WHERE CD.IndCierre = 1
          AND CD.TransBoveda = 0
          AND C.OficinaId = @OficinaId
          AND MC.Estado = 1
        GROUP BY C.Denominacion, CD.CajaDiarioId, MC.TipoPagoId
    ),
    CUENTAS AS (
        SELECT
            Caja,
            CajaDiarioId,
            TipoPagoId,
            SUM(Importe) AS Importe
        FROM MOVS_BASE
        GROUP BY Caja, CajaDiarioId, TipoPagoId
    )
    INSERT INTO CREDITO.BovedaMov (
        BovedaId,
        CodOperacion,
        Glosa,
        CajaDiarioId,
        TipoPagoId,
        Importe,
        IndEntrada,
        Estado,
        UsuarioRegId,
        FechaReg
    )
    SELECT
        @BovedaId,
        'TRE',
        @FechaTexto + CU.Caja,
        CU.CajaDiarioId,
        CU.TipoPagoId,
        ROUND(CU.Importe, 2),
        1,
        1,
        @UsuarioCierreId,
        @Fecha
    FROM CUENTAS CU
    WHERE ROUND(CU.Importe, 2) <> 0
    ORDER BY CU.CajaDiarioId, CU.TipoPagoId;

    IF @Sobrante > 0
        INSERT INTO CREDITO.BovedaMov (
            BovedaId,
            CodOperacion,
            Glosa,
            CajaDiarioId,
            TipoPagoId,
            Importe,
            IndEntrada,
            Estado,
            UsuarioRegId,
            FechaReg
        )
        VALUES (
            @BovedaId,
            'TRE',
            @FechaTexto + 'SOBRANTE',
            0,
            1,
            @Sobrante,
            1,
            1,
            @UsuarioCierreId,
            @Fecha
        );

    UPDATE CD
    SET CD.TransBoveda = 1
    FROM CREDITO.CajaDiario CD
    INNER JOIN CREDITO.Caja C ON C.CajaId = CD.CajaId
    WHERE CD.IndCierre = 1
      AND CD.TransBoveda = 0
      AND C.OficinaId = @OficinaId;

    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId;
END
GO

IF EXISTS (
    SELECT OficinaId
    FROM CREDITO.Boveda
    WHERE IndCierre = 0 AND IndTemporal = 0
    GROUP BY OficinaId
    HAVING COUNT(1) > 1
)
BEGIN
    RAISERROR(
        N'B-06 no se aplicó: hay más de una bóveda principal abierta en alguna oficina. Corrija y vuelva a ejecutar el índice.',
        16,
        1);
END
ELSE IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Boveda_Oficina_PrincipalAbierta'
      AND object_id = OBJECT_ID(N'CREDITO.Boveda')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Boveda_Oficina_PrincipalAbierta
        ON CREDITO.Boveda (OficinaId)
        WHERE IndCierre = 0 AND IndTemporal = 0;
END
GO
