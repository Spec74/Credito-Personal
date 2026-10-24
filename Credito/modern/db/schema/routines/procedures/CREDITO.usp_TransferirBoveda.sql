CREATE PROCEDURE [CREDITO].[usp_TransferirBoveda](
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

-- transferir boveda a boveda
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

		IF @TipoPagoId NOT BETWEEN 1 AND 5
		BEGIN
			RAISERROR(N'El medio debe ser una marca (1-5). No use tipos de sede (6-8).', 16, 1)
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
