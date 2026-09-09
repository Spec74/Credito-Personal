/*
[CREDITO].[usp_MovimientoCaja_Del] 1073812,'pruebas',3
*/
CREATE PROC [CREDITO].[usp_MovimientoCaja_Del] 
@MovimientoCajaId INT,
@Observacion VARCHAR(MAX),
@UsuarioId INT
AS
DECLARE @FechaActual DATETIME=dbo.ufnFecha()
DECLARE @Operacion CHAR(3),@CreditoId INT,@OrdenVentaId INT,@CajaDiarioId INT,@EstadoEnAlmacen INT=2
SELECT @Operacion=Operacion,@CajaDiarioId=CajaDiarioId 
FROM CREDITO.MovimientoCaja 
WHERE MovimientoCajaId=@MovimientoCajaId

INSERT INTO CREDITO.MovimientoCajaAnu ( MovimientoCajaId ,Observacion ,UsuarioRegId ,FechaReg)
VALUES  ( @MovimientoCajaId , @Observacion ,@UsuarioId ,@FechaActual)

IF @Operacion='CUO'
BEGIN	
	SELECT @CreditoId=CreditoId FROM CREDITO.MovimientoCaja WHERE MovimientoCajaId = @MovimientoCajaId
	UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId = @MovimientoCajaId	
	UPDATE CREDITO.Credito SET Estado='DES',UsuarioModId=@UsuarioId,FechaMod=@FechaActual WHERE CreditoId=@CreditoId AND Estado='PAG'
	EXEC CREDITO.usp_RegenerarPlanPago @CreditoId
END
ELSE
IF @Operacion='INI' OR @Operacion='GAD'
BEGIN	
	SELECT @CreditoId=CreditoId FROM CREDITO.CuentaxCobrar 
	WHERE MovimientoCajaId = @MovimientoCajaId
	
	UPDATE CREDITO.CuentaxCobrar SET MovimientoCajaId=NULL, Estado='PEN' WHERE MovimientoCajaId = @MovimientoCajaId	
	UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId = @MovimientoCajaId	
	UPDATE CREDITO.Credito SET Estado='DES',UsuarioModId=@UsuarioId,FechaMod=@FechaActual WHERE CreditoId=@CreditoId AND Estado='PAG'
END
ELSE
IF @Operacion='CON'
BEGIN	
	DECLARE @MovimientoAlmacenId INT
	SELECT @OrdenVentaId = ov.OrdenVentaId ,@MovimientoAlmacenId = OV.MovimientoAlmacenId
	FROM CREDITO.MovimientoCaja mc
	INNER JOIN VENTAS.OrdenVenta ov ON ov.OrdenVentaId = mc.OrdenVentaId
	WHERE mc.MovimientoCajaId = @MovimientoCajaId
			
	UPDATE CREDITO.CuentaxCobrar SET Estado='ANU' WHERE MovimientoCajaId = @MovimientoCajaId	
	UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId = @MovimientoCajaId	
	UPDATE VENTAS.OrdenVenta SET Estado=0,MovimientoAlmacenId=NULL ,UsuarioModId=@UsuarioId,FechaMod=@FechaActual
	WHERE OrdenVentaId=@OrdenVentaId 
	
	UPDATE	SA
	SET		SA.EstadoId=@EstadoEnAlmacen, 
			SA.MovimientoDetSalId = NULL
	FROM VENTAS.OrdenVentaDet OVD
	INNER JOIN VENTAS.OrdenVentaDetSerie OVDS ON OVD.OrdenVentaDetId = OVDS.OrdenVentaDetId
	INNER JOIN ALMACEN.SerieArticulo SA ON SA.SerieArticuloId=OVDS.SerieArticuloId
	WHERE OVD.OrdenVentaId=@OrdenVentaId
	
	DELETE FROM ALMACEN.MovimientoDet WHERE MovimientoId=@MovimientoAlmacenId
	DELETE FROM ALMACEN.Movimiento WHERE MovimientoId=@MovimientoAlmacenId
	
END
ELSE
IF @Operacion='TRE' OR @Operacion='TRS'
BEGIN	
	-- actualizar origen
	UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId = @MovimientoCajaId
	--actualizar destino
	DECLARE @desc VARCHAR(MAX)
	SELECT @desc=Descripcion FROM  CREDITO.MovimientoCaja WHERE MovimientoCajaId=@MovimientoCajaId
	IF CHARINDEX('[',@desc) > 0
	BEGIN
		DECLARE @ini INT = CHARINDEX('[',@desc) + 1
		DECLARE @Tipo VARCHAR(20) = (SELECT SUBSTRING(@desc,@ini,9))
		DECLARE @Mov INT = (SELECT SUBSTRING(@desc,@ini +10,CHARINDEX(']',@desc )-@ini -10 ))
		IF @Tipo='MovBoveda'
		BEGIN
			UPDATE CREDITO.BovedaMov SET Estado=0 WHERE MovimientoBovedaId=@Mov
			DECLARE @BovedaidDestino INT = (SELECT BovedaId FROM CREDITO.BovedaMov WHERE MovimientoBovedaId=@Mov)
			EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaidDestino
		END
		IF @Tipo='MovCajero'
		BEGIN
			UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId=@Mov
			DECLARE @cajaDiarioidDestino INT = (SELECT CajaDiarioId FROM CREDITO.MovimientoCaja WHERE MovimientoCajaId=@Mov)
			EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @cajaDiarioidDestino
		END
	END
END
ELSE
IF @Operacion='CDN'
BEGIN	
	SELECT @CreditoId=CreditoId FROM CREDITO.CuentaxCobrar 
	WHERE MovimientoCajaId = @MovimientoCajaId
	
	UPDATE CREDITO.CuentaxCobrar SET Estado='ANU' WHERE MovimientoCajaId = @MovimientoCajaId
	UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId = @MovimientoCajaId	
	UPDATE CREDITO.Credito SET Estado='DES',UsuarioModId=@UsuarioId,FechaMod=@FechaActual, Observacion='' 
	WHERE CreditoId=@CreditoId AND Estado='PAG'
END
ELSE
BEGIN
	UPDATE CREDITO.MovimientoCaja SET Estado=0 WHERE MovimientoCajaId = @MovimientoCajaId	
END

/*actualizar caja diario*/
EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId
