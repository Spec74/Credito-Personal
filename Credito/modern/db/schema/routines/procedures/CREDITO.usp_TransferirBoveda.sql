CREATE PROCEDURE [CREDITO].[usp_TransferirBoveda](
@BovedaInicioId INT,
@BovedaDestinoId INT,
@Glosa VARCHAR(MAX),
@Monto DECIMAL(16,2), 
@UsuarioRegId INT,
@flagAceptar INT, 
@BovedaMovTempId INT=0)
AS
DECLARE @Estado BIT,@FechaReg DATETIME				 		
SET @Estado= 1
SET @FechaReg = dbo.ufnFecha()

-- transferir boveda a boveda
IF @flagAceptar = 0
	BEGIN
		--Inserta BovedaMov Inicio
		INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion,Glosa,Importe,IndEntrada,Estado,UsuarioRegId,FechaReg)
					VALUES(@BovedaInicioId, 'TRS',@Glosa, @Monto,0,@Estado,@UsuarioRegId,@FechaReg)	
		--Actualiza  Boveda Inicio
		EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId
	
		-- Boveda Destino
		INSERT INTO CREDITO.BovedaMov (BovedaId, CodOperacion,Glosa,Importe,IndEntrada,Estado,UsuarioRegId,FechaReg)
		VALUES(@BovedaDestinoId, 'TRE',@Glosa, @Monto,1,@Estado,@UsuarioRegId,@FechaReg)	
								  
		EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaDestinoId
	END

IF @flagAceptar = 2
	BEGIN
	--Elimino BovedaMov Inicio
	DELETE CREDITO.BovedaMov WHERE MovimientoBovedaId = (SELECT MovimientoBovedaIniId FROM CREDITO.BovedaMovTemp
														 WHERE BovedaMovTempId = @BovedaMovTempId )
	--Actualiza  Boveda Inicio
	SET @BovedaInicioId = (SELECT BovedaInicioId  FROM CREDITO.BovedaMovTemp WHERE BovedaMovTempId = @BovedaMovTempId)
	
	EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaInicioId

	--Elimina BovedaMovTemp
	DELETE CREDITO.BovedaMovTemp where BovedaMovTempId = @BovedaMovTempId
	END
