CREATE PROC [ALMACEN].[usp_Movimiento_Upd]
	@Flag INT = 1,
	@MovimientoId INT,
	@TipoMovimientoId INT=0,
	@FechaMov DATE=NULL,
	@Observacion VARCHAR(MAX)=NULL
AS

DECLARE @Retorno VARCHAR(50) = ''

IF @Flag=1 --DESCONFIRMAR MOVIMIENTO
BEGIN
	
	IF NOT EXISTS(SELECT 1 FROM ALMACEN.MovimientoDet MD
				INNER JOIN ALMACEN.SerieArticulo SA ON MD.MovimientoDetId = SA.MovimientoDetEntId
				WHERE MD.MovimientoId=@MovimientoId AND SA.EstadoId<>2
			)
	BEGIN
		
		UPDATE ALMACEN.Movimiento SET EstadoId=1
		WHERE MovimientoId=@MovimientoId
		
		UPDATE SA
		SET SA.EstadoId = 1
		FROM ALMACEN.MovimientoDet MD
		INNER JOIN ALMACEN.SerieArticulo SA ON MD.MovimientoDetId = SA.MovimientoDetEntId
		WHERE MD.MovimientoId=@MovimientoId
		
		SET @Retorno='UPD'
	END
	
	SELECT ISNULL(@Retorno,'') 'Retorno'
	RETURN
END
IF @Flag=2 --ACTUALIZAR MOVIMIENTO
BEGIN
	
	UPDATE	ALMACEN.Movimiento 
	SET		TipoMovimientoId=@TipoMovimientoId,
			Fecha=@FechaMov,
			Observacion=@Observacion
	WHERE	MovimientoId=@MovimientoId
		
	SELECT '' 'Retorno'

	RETURN
END
IF @Flag=3 --CONFIRMAR MOVIMIENTO
BEGIN
	
	IF NOT EXISTS( SELECT 1 FROM ALMACEN.MovimientoDet WHERE MovimientoId=@MovimientoId)
	BEGIN
		SELECT 'Esta Acción requiere que ingrese un Detalle.' 'Retorno'
		RETURN 
	END
	
	UPDATE ALMACEN.Movimiento SET EstadoId=2
	WHERE MovimientoId=@MovimientoId
	
	UPDATE SA
	SET SA.EstadoId = 2
	FROM ALMACEN.MovimientoDet MD
	INNER JOIN ALMACEN.SerieArticulo SA ON MD.MovimientoDetId = SA.MovimientoDetEntId
	WHERE MD.MovimientoId=@MovimientoId
	
	SELECT '' 'Retorno'
	RETURN
END
